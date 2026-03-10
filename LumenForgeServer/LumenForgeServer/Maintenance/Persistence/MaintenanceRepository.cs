using LumenForgeServer.Common.Database;
using LumenForgeServer.Maintenance.Domain;
using Microsoft.EntityFrameworkCore;

namespace LumenForgeServer.Maintenance.Persistence;

/// <summary>
/// EF Core-backed repository for maintenance entities.
/// </summary>
public sealed class MaintenanceRepository(AppDbContext db) : IMaintenanceRepository
{
    public Task AddStatusAsync(MaintenanceBacklogStatus status, CancellationToken ct)
        => db.MaintenanceBacklogStatuses.AddAsync(status, ct).AsTask();

    public Task<MaintenanceBacklogStatus?> GetStatusByUuidAsync(Guid uuid, CancellationToken ct)
        => db.MaintenanceBacklogStatuses.SingleOrDefaultAsync(s => s.Uuid == uuid, ct);

    public async Task<IReadOnlyList<MaintenanceBacklogStatus>> ListStatusesAsync(
        string? search, int limit, int offset, CancellationToken ct)
    {
        var query = db.MaintenanceBacklogStatuses.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s =>
                s.Name.Contains(search) ||
                (s.Description != null && s.Description.Contains(search)));
        }

        return await query
            .OrderBy(s => s.Name)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(ct);
    }

    public Task DeleteStatusAsync(MaintenanceBacklogStatus status, CancellationToken ct)
    {
        db.MaintenanceBacklogStatuses.Remove(status);
        return Task.CompletedTask;
    }

    public Task<long?> TryGetStatusIdByUuidAsync(Guid uuid, CancellationToken ct)
        => db.MaintenanceBacklogStatuses
            .Where(s => s.Uuid == uuid)
            .Select(s => (long?)s.Id)
            .SingleOrDefaultAsync(ct);

    public Task<bool> StatusHasBacklogsAsync(long statusId, CancellationToken ct)
        => db.MaintenanceBacklogs.AnyAsync(b => b.MaintenanceBacklogStatusId == statusId, ct);

    public Task AddBacklogAsync(MaintenanceBacklog backlog, CancellationToken ct)
        => db.MaintenanceBacklogs.AddAsync(backlog, ct).AsTask();

    public Task<MaintenanceBacklog?> GetBacklogByUuidAsync(Guid uuid, CancellationToken ct)
        => BuildBacklogGraphQuery().SingleOrDefaultAsync(b => b.Uuid == uuid, ct);

    public async Task<(IReadOnlyList<MaintenanceBacklog> items, long total)> ListBacklogsAsync(
        string? search, Guid? statusUuid, bool unresolvedOnly, int limit, int offset, CancellationToken ct)
    {
        var query = BuildBacklogGraphQuery().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(b =>
                b.IssueSummary.Contains(search) ||
                (b.IssueDescription != null && b.IssueDescription.Contains(search)) ||
                (b.Device != null && (b.Device.SerialNumber.Contains(search) ||
                    (b.Device.DeviceName != null && b.Device.DeviceName.Contains(search)))));
        }

        if (statusUuid.HasValue)
        {
            query = query.Where(b => b.MaintenanceBacklogStatus.Uuid == statusUuid.Value);
        }

        if (unresolvedOnly)
        {
            query = query.Where(b => b.ResolvedAt == null);
        }

        var total = await query.LongCountAsync(ct);

        var items = await query
            .OrderByDescending(b => b.ReportedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<IReadOnlyList<MaintenanceBacklog>> GetBacklogsByDeviceIdAsync(long deviceId, CancellationToken ct)
        => await BuildBacklogGraphQuery()
            .Where(b => b.DeviceId == deviceId)
            .OrderByDescending(b => b.ReportedAt)
            .ToListAsync(ct);

    public Task DeleteBacklogAsync(MaintenanceBacklog backlog, CancellationToken ct)
    {
        db.MaintenanceBacklogs.Remove(backlog);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct)
        => db.SaveChangesAsync(ct);

    private IQueryable<MaintenanceBacklog> BuildBacklogGraphQuery()
        => db.MaintenanceBacklogs
            .Include(b => b.MaintenanceBacklogStatus)
            .Include(b => b.Device)
            .Include(b => b.RentalItem)
            .Include(b => b.ChecklistItem)
            .AsSplitQuery();
}
