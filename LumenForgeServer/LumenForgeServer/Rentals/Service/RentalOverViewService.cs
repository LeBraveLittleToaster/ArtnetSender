using LumenForgeServer.Common.Exceptions;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Dto.Query;
using LumenForgeServer.Rentals.Dto.View;
using LumenForgeServer.Rentals.Persistence;
using NodaTime;

namespace LumenForgeServer.Rentals.Service;

public sealed class RentalOverViewService(IRentalProcessRepository repository)
{
    private static readonly HashSet<RentalStage> TerminalStages =
    [
        RentalStage.Completed,
        RentalStage.Cancelled,
        RentalStage.Scrapped
    ];

    public async Task<(List<RentalProcessSummaryView> list, int total)> ListProcessesAsync(
        RentalListQueryDto query,
        bool fullAccess,
        string? callerKcId,
        CancellationToken ct)
    {
        var scopedQuery = fullAccess
            ? query
            : query with
            {
                OwnerKcId = callerKcId
                    ?? throw new UnauthorizedAccessException("Unable to resolve caller identity.")
            };

        var (items, total) = await repository.ListAsync(scopedQuery, ct);
        return (items.Select(RentalProcessSummaryView.FromEntity).ToList(), total);
    }

    public async Task<RentalProcessView> GetProcessAsync(
        Guid processGuid,
        RentalProcessInclude includes,
        CancellationToken ct)
    {
        var process = includes == RentalProcessInclude.None
            ? await repository.GetByGuidAsync(processGuid, ct)
            : await repository.GetByGuidWithIncludesAsync(processGuid, includes, ct);

        if (process is null)
            throw new NotFoundException($"Process instance '{processGuid}' not found.");

        return RentalProcessView.FromEntity(process, includes);
    }

    public async Task<(List<RentalActionLogView> list, int total)> GetProcessHistoryAsync(
        Guid processGuid,
        int limit,
        int offset,
        CancellationToken ct)
    {
        _ = await repository.GetByGuidAsync(processGuid, ct)
            ?? throw new NotFoundException($"Process instance '{processGuid}' not found.");

        var (logs, total) = await repository.GetActionLogsByProcessGuidAsync(processGuid, limit, offset, ct);
        return (logs.Select(RentalActionLogView.FromEntity).ToList(), total);
    }

    public async Task<RentalOverviewDto> GetOverviewAsync(CancellationToken ct)
    {
        var byStage = await repository.CountByStageAsync(ct);
        var totalProcesses = byStage.Values.Sum();
        var terminalCount = byStage
            .Where(kv => TerminalStages.Contains(kv.Key))
            .Sum(kv => kv.Value);

        return new RentalOverviewDto
        {
            TotalProcesses = totalProcesses,
            ByStage = byStage,
            ActiveCount = totalProcesses - terminalCount,
            TerminalCount = terminalCount,
            TotalDamageReports = await repository.CountDamageReportsAsync(ct),
            TotalExtensionRequests = await repository.CountExtensionsAsync(ct),
            PendingExtensions = await repository.CountPendingExtensionsAsync(ct),
            TotalActionLogs = await repository.CountActionLogsAsync(ct)
        };
    }

    public async Task<RentalRecentActivityDto> GetRecentActivityAsync(int days, CancellationToken ct)
    {
        var since = SystemClock.Instance.GetCurrentInstant() - Duration.FromDays(days);

        return new RentalRecentActivityDto
        {
            ProcessesCreated = await repository.CountProcessesCreatedSinceAsync(since, ct),
            ProcessesCompleted = await repository.CountProcessesReachedStageSinceAsync(
                RentalStage.Completed, since, ct),
            ProcessesCancelled = await repository.CountProcessesReachedStageSinceAsync(
                RentalStage.Cancelled, since, ct),
            ActionsPerformed = await repository.CountActionLogsSinceAsync(since, ct),
            DamagesReported = await repository.CountDamageReportsSinceAsync(since, ct),
            WindowDays = days
        };
    }

    public async Task<List<StageBucketDto>> GetByStageAsync(CancellationToken ct)
    {
        var byStage = await repository.CountByStageAsync(ct);

        return Enum.GetValues<RentalStage>()
            .Select(stage => new StageBucketDto
            {
                Stage = stage,
                Count = byStage.GetValueOrDefault(stage, 0)
            })
            .ToList();
    }
}
