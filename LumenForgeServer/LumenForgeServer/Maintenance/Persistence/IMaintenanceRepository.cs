using LumenForgeServer.Maintenance.Domain;

namespace LumenForgeServer.Maintenance.Persistence;

/// <summary>
/// Persistence contract for maintenance entities.
/// </summary>
public interface IMaintenanceRepository
{
    // Backlog statuses
    Task AddStatusAsync(MaintenanceBacklogStatus status, CancellationToken ct);
    Task<MaintenanceBacklogStatus?> GetStatusByUuidAsync(Guid uuid, CancellationToken ct);
    Task<IReadOnlyList<MaintenanceBacklogStatus>> ListStatusesAsync(string? search, int limit, int offset, CancellationToken ct);
    Task DeleteStatusAsync(MaintenanceBacklogStatus status, CancellationToken ct);
    Task<long?> TryGetStatusIdByUuidAsync(Guid uuid, CancellationToken ct);
    Task<bool> StatusHasBacklogsAsync(long statusId, CancellationToken ct);

    // Backlog entries
    Task AddBacklogAsync(MaintenanceBacklog backlog, CancellationToken ct);
    Task<MaintenanceBacklog?> GetBacklogByUuidAsync(Guid uuid, CancellationToken ct);
    Task<(IReadOnlyList<MaintenanceBacklog> items, long total)> ListBacklogsAsync(
        string? search, Guid? statusUuid, bool unresolvedOnly, int limit, int offset, CancellationToken ct);
    Task<IReadOnlyList<MaintenanceBacklog>> GetBacklogsByDeviceIdAsync(long deviceId, CancellationToken ct);
    Task DeleteBacklogAsync(MaintenanceBacklog backlog, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
