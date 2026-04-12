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
    /// <summary>
    /// Executes the add job async operation.
    /// Core concept: maps application requests to persistence queries and materializes domain data.
    /// </summary>
    /// <remarks>Potential side effects: may execute database writes or update tracked entity state in the current DbContext.</remarks>
    /// <param name="job">Numeric input used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task AddJobAsync(MaintenanceJob job, CancellationToken ct)
        => db.MaintenanceJobs.AddAsync(job, ct).AsTask();

    /// <summary>
    /// Executes the get job by guid async operation.
    /// Core concept: maps application requests to persistence queries and materializes domain data.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="jobGuid">Unique identifier used to target the requested entity.</param>
    /// <param name="include">Numeric input used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the MaintenanceJob? result.</returns>
    public Task<MaintenanceJob?> GetJobByGuidAsync(Guid jobGuid, MaintenanceJobInclude include, CancellationToken ct)
        => BuildJobQuery(include).SingleOrDefaultAsync(j => j.Guid == jobGuid, ct);

    /// <summary>
    /// Executes the get jobs by guids async operation.
    /// Core concept: maps application requests to persistence queries and materializes domain data.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="jobGuids">Unique identifier used to target the requested entity.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the IReadOnlyList&lt;MaintenanceJob&gt; result.</returns>
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

    /// <summary>
    /// Executes the try get rental id by guid async operation.
    /// Core concept: maps application requests to persistence queries and materializes domain data.
    /// </summary>
    /// <remarks>Potential side effects: may execute database writes or update tracked entity state in the current DbContext.</remarks>
    /// <param name="rentalGuid">Unique identifier used to target the requested entity.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the long? result.</returns>
    public Task<long?> TryGetRentalIdByGuidAsync(Guid rentalGuid, CancellationToken ct)
        => db.Rentals
            .Where(r => r.Uuid == rentalGuid)
            .Select(r => (long?)r.Id)
            .SingleOrDefaultAsync(ct);

    /// <summary>
    /// Executes the task operation.
    /// Core concept: maps application requests to persistence queries and materializes domain data.
    /// </summary>
    /// <remarks>Potential side effects: may execute database writes or update tracked entity state in the current DbContext.</remarks>
    /// <param name="search">Text input used by this operation.</param>
    /// <param name="status">Numeric input used by this operation.</param>
    /// <param name="unresolvedOnly">Boolean flag controlling the operation behavior.</param>
    /// <param name="limit">Numeric input used by this operation.</param>
    /// <param name="offset">Numeric input used by this operation.</param>
    /// <param name="include">Numeric input used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>The operation result.</returns>
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

    /// <summary>
    /// Executes the delete job async operation.
    /// Core concept: maps application requests to persistence queries and materializes domain data.
    /// </summary>
    /// <remarks>Potential side effects: may execute database writes or update tracked entity state in the current DbContext.</remarks>
    /// <param name="job">Numeric input used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task DeleteJobAsync(MaintenanceJob job, CancellationToken ct)
    {
        db.MaintenanceJobs.Remove(job);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Executes the add task async operation.
    /// Core concept: maps application requests to persistence queries and materializes domain data.
    /// </summary>
    /// <remarks>Potential side effects: may execute database writes or update tracked entity state in the current DbContext.</remarks>
    /// <param name="task">Numeric input used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task AddTaskAsync(MaintenanceTask task, CancellationToken ct)
        => db.MaintenanceTasks.AddAsync(task, ct).AsTask();

    /// <summary>
    /// Executes the get task by guid async operation.
    /// Core concept: maps application requests to persistence queries and materializes domain data.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="taskGuid">Unique identifier used to target the requested entity.</param>
    /// <param name="include">Numeric input used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the MaintenanceTask? result.</returns>
    public Task<MaintenanceTask?> GetTaskByGuidAsync(Guid taskGuid, MaintenanceTaskInclude include, CancellationToken ct)
        => BuildTaskQuery(include).SingleOrDefaultAsync(t => t.Guid == taskGuid, ct);

    /// <summary>
    /// Executes the task operation.
    /// Core concept: maps application requests to persistence queries and materializes domain data.
    /// </summary>
    /// <remarks>Potential side effects: may execute database writes or update tracked entity state in the current DbContext.</remarks>
    /// <param name="jobGuid">Unique identifier used to target the requested entity.</param>
    /// <param name="limit">Numeric input used by this operation.</param>
    /// <param name="offset">Numeric input used by this operation.</param>
    /// <param name="include">Numeric input used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>The operation result.</returns>
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

    /// <summary>
    /// Executes the delete task async operation.
    /// Core concept: maps application requests to persistence queries and materializes domain data.
    /// </summary>
    /// <remarks>Potential side effects: may execute database writes or update tracked entity state in the current DbContext.</remarks>
    /// <param name="task">Numeric input used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task DeleteTaskAsync(MaintenanceTask task, CancellationToken ct)
    {
        db.MaintenanceTasks.Remove(task);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Executes the add log entry async operation.
    /// Core concept: maps application requests to persistence queries and materializes domain data.
    /// </summary>
    /// <remarks>Potential side effects: may execute database writes or update tracked entity state in the current DbContext.</remarks>
    /// <param name="logEntry">Numeric input used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task AddLogEntryAsync(MaintenanceLogEntry logEntry, CancellationToken ct)
        => db.MaintenanceLogEntries.AddAsync(logEntry, ct).AsTask();

    /// <summary>
    /// Executes the task operation.
    /// Core concept: maps application requests to persistence queries and materializes domain data.
    /// </summary>
    /// <remarks>Potential side effects: may execute database writes or update tracked entity state in the current DbContext.</remarks>
    /// <param name="taskGuid">Unique identifier used to target the requested entity.</param>
    /// <param name="limit">Numeric input used by this operation.</param>
    /// <param name="offset">Numeric input used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>The operation result.</returns>
    public async Task<(IReadOnlyList<MaintenanceLogEntry> items, long total)> ListLogsForTaskAsync(Guid taskGuid, int limit, int offset, CancellationToken ct)
    {
        var query = db.MaintenanceLogEntries
            .Where(l => l.MaintenanceTask.Guid == taskGuid)
            .OrderBy(l => l.CreatedAt);

        var total = await query.LongCountAsync(ct);
        var items = await query
            .AsNoTracking()
            .Skip(offset)
            .Take(limit)
            .ToListAsync(ct);

        return (items, total);
    }

    /// <summary>
    /// Executes the save changes async operation.
    /// Core concept: maps application requests to persistence queries and materializes domain data.
    /// </summary>
    /// <remarks>Potential side effects: may execute database writes or update tracked entity state in the current DbContext.</remarks>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SaveChangesAsync(CancellationToken ct)
        => db.SaveChangesAsync(ct);

    /// <summary>
    /// Executes the build job query operation.
    /// Core concept: maps application requests to persistence queries and materializes domain data.
    /// </summary>
    /// <remarks>Potential side effects: may execute database writes or update tracked entity state in the current DbContext.</remarks>
    /// <param name="include">Numeric input used by this operation.</param>
    /// <returns>The operation result.</returns>
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

    /// <summary>
    /// Executes the build task query operation.
    /// Core concept: maps application requests to persistence queries and materializes domain data.
    /// </summary>
    /// <remarks>Potential side effects: may execute database writes or update tracked entity state in the current DbContext.</remarks>
    /// <param name="include">Numeric input used by this operation.</param>
    /// <returns>The operation result.</returns>
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
