using LumenForgeServer.Common.Database;
using LumenForgeServer.Maintenance.Domain;
using LumenForgeServer.Maintenance.Dto.Query;
using Microsoft.EntityFrameworkCore;

namespace LumenForgeServer.Maintenance.Persistence;

/// <summary>
/// EF Core-backed repository for maintenance entities.
/// </summary>
public sealed class MaintenanceRepository(AppDbContext db) : IMaintenanceRepository
{
    public Task AddJobAsync(MaintenanceJob job, CancellationToken ct)
        => db.MaintenanceJobs.AddAsync(job, ct).AsTask();

    public Task<MaintenanceJob?> GetJobByGuidAsync(Guid jobGuid, MaintenanceJobInclude include, CancellationToken ct)
        => BuildJobQuery(include).SingleOrDefaultAsync(j => j.Guid == jobGuid, ct);

    public async Task<IReadOnlyList<MaintenanceJob>> GetJobsByGuidsAsync(IReadOnlyCollection<Guid> jobGuids, CancellationToken ct)
    {
        if (jobGuids.Count == 0)
        {
            return [];
        }

        return await BuildJobQuery(MaintenanceJobInclude.None)
            .Where(j => jobGuids.Contains(j.Guid))
            .ToListAsync(ct);
    }

    public Task<long?> TryGetRentalIdByGuidAsync(Guid rentalGuid, CancellationToken ct)
        => db.Rentals
            .Where(r => r.Uuid == rentalGuid)
            .Select(r => (long?)r.Id)
            .SingleOrDefaultAsync(ct);

    public async Task<(IReadOnlyList<MaintenanceJob> items, long total)> ListJobsAsync(
        string? search,
        MaintenanceStatus? status,
        bool unresolvedOnly,
        int limit,
        int offset,
        MaintenanceJobInclude include,
        CancellationToken ct)
    {
        var query = BuildJobQuery(include).AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(j =>
                j.Name.Contains(search) ||
                j.Description.Contains(search) ||
                j.CreatedByUserKcId.Contains(search));
        }

        if (status.HasValue)
        {
            query = query.Where(j => j.Status == status.Value);
        }

        if (unresolvedOnly)
        {
            query = query.Where(j => j.ResolvedAt == null);
        }

        var total = await query.LongCountAsync(ct);
        var items = await query
            .OrderByDescending(j => j.ReportedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task DeleteJobAsync(MaintenanceJob job, CancellationToken ct)
    {
        db.MaintenanceJobs.Remove(job);
        return Task.CompletedTask;
    }

    public Task AddTaskAsync(MaintenanceTask task, CancellationToken ct)
        => db.MaintenanceTasks.AddAsync(task, ct).AsTask();

    public Task<MaintenanceTask?> GetTaskByGuidAsync(Guid taskGuid, MaintenanceTaskInclude include, CancellationToken ct)
        => BuildTaskQuery(include).SingleOrDefaultAsync(t => t.Guid == taskGuid, ct);

    public async Task<(IReadOnlyList<MaintenanceTask> items, long total)> ListTasksForJobAsync(
        Guid jobGuid,
        int limit,
        int offset,
        MaintenanceTaskInclude include,
        CancellationToken ct)
    {
        var query = BuildTaskQuery(include)
            .AsNoTracking()
            .Where(t => t.MaintenanceJob.Guid == jobGuid)
            .OrderBy(t => t.CreatedAt);

        var total = await query.LongCountAsync(ct);
        var items = await query
            .Skip(offset)
            .Take(limit)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task DeleteTaskAsync(MaintenanceTask task, CancellationToken ct)
    {
        db.MaintenanceTasks.Remove(task);
        return Task.CompletedTask;
    }

    public Task AddLogEntryAsync(MaintenanceLogEntry logEntry, CancellationToken ct)
        => db.MaintenanceLogEntries.AddAsync(logEntry, ct).AsTask();

    public async Task<IReadOnlyList<MaintenanceLogEntry>> ListLogsForTaskAsync(Guid taskGuid, CancellationToken ct)
        => await db.MaintenanceLogEntries
            .Where(l => l.MaintenanceTask.Guid == taskGuid)
            .OrderBy(l => l.CreatedAt)
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct)
        => db.SaveChangesAsync(ct);

    private IQueryable<MaintenanceJob> BuildJobQuery(MaintenanceJobInclude include)
    {
        var query = db.MaintenanceJobs.AsQueryable();

        if (include.HasFlag(MaintenanceJobInclude.Devices))
        {
            query = query.Include(j => j.AffectedDevices);
        }

        if (include.HasFlag(MaintenanceJobInclude.RelatedJobs))
        {
            query = query.Include(j => j.RelatedJobs);
        }

        if (include.HasFlag(MaintenanceJobInclude.RelatedRental))
        {
            query = query.Include(j => j.RelatedToRental);
        }

        if (include.HasFlag(MaintenanceJobInclude.Tasks) || include.HasFlag(MaintenanceJobInclude.Logs))
        {
            query = query.Include(j => j.Tasks)
                .ThenInclude(t => t.AffectedDevices);

            if (include.HasFlag(MaintenanceJobInclude.Logs))
            {
                query = query.Include(j => j.Tasks)
                    .ThenInclude(t => t.Log);
            }
        }

        return query.AsSplitQuery();
    }

    private IQueryable<MaintenanceTask> BuildTaskQuery(MaintenanceTaskInclude include)
    {
        var query = db.MaintenanceTasks
            .Include(t => t.MaintenanceJob)
            .AsQueryable();

        if (include.HasFlag(MaintenanceTaskInclude.Devices))
        {
            query = query.Include(t => t.AffectedDevices);
        }

        if (include.HasFlag(MaintenanceTaskInclude.Logs))
        {
            query = query.Include(t => t.Log);
        }

        return query.AsSplitQuery();
    }
}
