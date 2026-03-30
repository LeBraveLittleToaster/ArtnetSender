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
        RentalAccessFilter accessFilter,
        CancellationToken ct)
    {
        var (items, total) = await repository.ListAsync(query, accessFilter, ct);
        return (items.Select(RentalProcessSummaryView.FromEntity).ToList(), total);
    }

    public async Task<RentalProcessView> GetProcessAsync(
        Guid processGuid,
        RentalProcessInclude includes,
        RentalAccessFilter accessFilter,
        CancellationToken ct)
    {
        var process = await repository.GetByGuidWithIncludesScopedAsync(processGuid, includes, accessFilter, ct);

        if (process is null)
            throw new NotFoundException($"Process instance '{processGuid}' not found.");

        return RentalProcessView.FromEntity(process, includes);
    }

    public async Task<(List<RentalActionLogView> list, int total)> GetProcessHistoryAsync(
        Guid processGuid,
        int limit,
        int offset,
        RentalAccessFilter accessFilter,
        CancellationToken ct)
    {
        _ = await repository.GetByGuidWithIncludesScopedAsync(
                processGuid,
                RentalProcessInclude.None,
                accessFilter,
                ct)
            ?? throw new NotFoundException($"Process instance '{processGuid}' not found.");

        var (logs, total) = await repository.GetActionLogsByProcessGuidAsync(processGuid, limit, offset, ct);
        return (logs.Select(RentalActionLogView.FromEntity).ToList(), total);
    }

    public async Task<RentalOverviewDto> GetOverviewAsync(RentalAccessFilter accessFilter, CancellationToken ct)
    {
        var byStage = await repository.CountByStageAsync(accessFilter, ct);
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
            TotalDamageReports = await repository.CountDamageReportsAsync(accessFilter, ct),
            TotalExtensionRequests = await repository.CountExtensionsAsync(accessFilter, ct),
            PendingExtensions = await repository.CountPendingExtensionsAsync(accessFilter, ct),
            TotalActionLogs = await repository.CountActionLogsAsync(accessFilter, ct)
        };
    }

    public async Task<RentalRecentActivityDto> GetRecentActivityAsync(
        int days,
        RentalAccessFilter accessFilter,
        CancellationToken ct)
    {
        var since = SystemClock.Instance.GetCurrentInstant() - Duration.FromDays(days);

        return new RentalRecentActivityDto
        {
            ProcessesCreated = await repository.CountProcessesCreatedSinceAsync(since, accessFilter, ct),
            ProcessesCompleted = await repository.CountProcessesReachedStageSinceAsync(
                RentalStage.Completed, since, accessFilter, ct),
            ProcessesCancelled = await repository.CountProcessesReachedStageSinceAsync(
                RentalStage.Cancelled, since, accessFilter, ct),
            ActionsPerformed = await repository.CountActionLogsSinceAsync(since, accessFilter, ct),
            DamagesReported = await repository.CountDamageReportsSinceAsync(since, accessFilter, ct),
            WindowDays = days
        };
    }

    public async Task<List<StageBucketDto>> GetByStageAsync(RentalAccessFilter accessFilter, CancellationToken ct)
    {
        var byStage = await repository.CountByStageAsync(accessFilter, ct);

        return Enum.GetValues<RentalStage>()
            .Select(stage => new StageBucketDto
            {
                Stage = stage,
                Count = byStage.GetValueOrDefault(stage, 0)
            })
            .ToList();
    }
}
