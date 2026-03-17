using LumenForgeServer.Maintenance.Domain;
using LumenForgeServer.Maintenance.Dto.Query;

namespace LumenForgeServer.Maintenance.Persistence;

/// <summary>
/// Persistence contract for maintenance jobs, tasks, and task logs.
/// </summary>
public interface IMaintenanceRepository
{
    Task AddJobAsync(MaintenanceJob job, CancellationToken ct);
    Task<MaintenanceJob?> GetJobByGuidAsync(Guid jobGuid, MaintenanceJobInclude include, CancellationToken ct);
    Task<IReadOnlyList<MaintenanceJob>> GetJobsByGuidsAsync(IReadOnlyCollection<Guid> jobGuids, CancellationToken ct);
    Task<long?> TryGetRentalIdByGuidAsync(Guid rentalGuid, CancellationToken ct);
    Task<(IReadOnlyList<MaintenanceJob> items, long total)> ListJobsAsync(
        string? search,
        MaintenanceStatus? status,
        bool unresolvedOnly,
        int limit,
        int offset,
        MaintenanceJobInclude include,
        CancellationToken ct);
    Task DeleteJobAsync(MaintenanceJob job, CancellationToken ct);

    Task AddTaskAsync(MaintenanceTask task, CancellationToken ct);
    Task<MaintenanceTask?> GetTaskByGuidAsync(Guid taskGuid, MaintenanceTaskInclude include, CancellationToken ct);
    Task<(IReadOnlyList<MaintenanceTask> items, long total)> ListTasksForJobAsync(
        Guid jobGuid,
        int limit,
        int offset,
        MaintenanceTaskInclude include,
        CancellationToken ct);
    Task DeleteTaskAsync(MaintenanceTask task, CancellationToken ct);

    Task AddLogEntryAsync(MaintenanceLogEntry logEntry, CancellationToken ct);
    Task<(IReadOnlyList<MaintenanceLogEntry> items, long total)> ListLogsForTaskAsync(Guid taskGuid, int limit, int offset, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
