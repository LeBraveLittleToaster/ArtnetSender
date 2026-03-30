using LumenForgeServer.Common.Database;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Dto.Query;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace LumenForgeServer.Rentals.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IRentalProcessRepository"/>.
/// Scoped to the current <see cref="AppDbContext"/> unit of work.
/// </summary>
public class RentalProcessRepository(AppDbContext db) : IRentalProcessRepository
{
    private IQueryable<RentalProcessInstance> BuildProcessWithIncludesQuery(RentalProcessInclude includes)
    {
        var query = db.RentalProcessInstances
            .Include(p => p.Rental)
            .ThenInclude(r => r!.Answers)
            .ThenInclude(a => a.Question)
            .AsQueryable();

        if (includes.HasFlag(RentalProcessInclude.Checklists))
            query = query.Include(p => p.Checklists).ThenInclude(c => c.Items);

        if (includes.HasFlag(RentalProcessInclude.Extensions))
            query = query.Include(p => p.Extensions);

        if (includes.HasFlag(RentalProcessInclude.DamageReports))
            query = query.Include(p => p.DamageReports);

        return query;
    }

    private static IQueryable<RentalProcessInstance> ApplyAccessScope(
        IQueryable<RentalProcessInstance> query,
        RentalAccessFilter accessFilter)
    {
        if (accessFilter.AllowAll)
            return query;

        var hasOwnerScope = !string.IsNullOrWhiteSpace(accessFilter.OwnerKcId);
        var ownerKcId = accessFilter.OwnerKcId;
        var groupGuids = accessFilter.GroupGuids.Distinct().ToArray();
        var hasGroupScope = groupGuids.Length > 0;

        if (!hasOwnerScope && !hasGroupScope)
            return query.Where(_ => false);

        return query.Where(p =>
            p.Rental != null &&
            ((hasOwnerScope && p.Rental.CustomerKcId == ownerKcId!) ||
             (hasGroupScope && p.Rental.GroupGuid.HasValue && groupGuids.Contains(p.Rental.GroupGuid.Value))));
    }

    /// <inheritdoc />
    public async Task<RentalProcessInstance?> GetByGuidAsync(Guid processGuid, CancellationToken ct)
    {
        return await db.RentalProcessInstances
            .Include(p => p.Rental)
            .ThenInclude(r => r!.Answers)
            .ThenInclude(a => a.Question)
            .FirstOrDefaultAsync(p => p.Guid == processGuid, ct);
    }

    /// <inheritdoc />
    public async Task<RentalProcessInstance?> GetByGuidWithDetailsAsync(Guid processGuid, CancellationToken ct)
    {
        return await db.RentalProcessInstances
            .Include(p => p.Rental)
            .ThenInclude(r => r!.Answers)
            .ThenInclude(a => a.Question)
            .Include(p => p.Checklists).ThenInclude(c => c.Items)
            .Include(p => p.Extensions)
            .Include(p => p.DamageReports)
            .FirstOrDefaultAsync(p => p.Guid == processGuid, ct);
    }

    /// <inheritdoc />
    public async Task<RentalProcessInstance?> GetByGuidWithIncludesAsync(
        Guid processGuid, RentalProcessInclude includes, CancellationToken ct)
    {
        var query = BuildProcessWithIncludesQuery(includes);
        return await query.FirstOrDefaultAsync(p => p.Guid == processGuid, ct);
    }

    /// <inheritdoc />
    public async Task<RentalProcessInstance?> GetByGuidWithIncludesScopedAsync(
        Guid processGuid,
        RentalProcessInclude includes,
        RentalAccessFilter accessFilter,
        CancellationToken ct)
    {
        var query = BuildProcessWithIncludesQuery(includes);
        query = ApplyAccessScope(query, accessFilter);
        return await query.FirstOrDefaultAsync(p => p.Guid == processGuid, ct);
    }

    /// <inheritdoc />
    public async Task AddAsync(RentalProcessInstance instance, CancellationToken ct)
    {
        await db.RentalProcessInstances.AddAsync(instance, ct);
    }

    /// <inheritdoc />
    public Task UpdateAsync(RentalProcessInstance instance, CancellationToken ct)
    {
        var entry = db.Entry(instance);
        if (entry.State == EntityState.Detached)
            db.RentalProcessInstances.Update(instance);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task AddActionLogAsync(RentalActionLog log, CancellationToken ct)
    {
        await db.RentalActionLogs.AddAsync(log, ct);
    }

    /// <inheritdoc />
    public async Task AddRentalAsync(Rental rental, CancellationToken ct)
    {
        await db.Rentals.AddAsync(rental, ct);
    }

    /// <inheritdoc />
    public async Task AddChecklistAsync(Checklist checklist, CancellationToken ct)
    {
        await db.Checklists.AddAsync(checklist, ct);
    }

    /// <inheritdoc />
    public async Task<Checklist?> GetChecklistByGuidAsync(Guid checklistGuid, CancellationToken ct)
    {
        return await db.Checklists
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Guid == checklistGuid, ct);
    }

    /// <inheritdoc />
    public async Task AddExtensionAsync(RentalExtension extension, CancellationToken ct)
    {
        await db.RentalExtensions.AddAsync(extension, ct);
    }

    /// <inheritdoc />
    public async Task<RentalExtension?> GetExtensionByGuidAsync(Guid extensionGuid, CancellationToken ct)
    {
        return await db.RentalExtensions
            .FirstOrDefaultAsync(e => e.Guid == extensionGuid, ct);
    }

    /// <inheritdoc />
    public async Task AddDamageReportsAsync(IEnumerable<RentalDamageReport> reports, CancellationToken ct)
    {
        await db.RentalDamageReports.AddRangeAsync(reports, ct);
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken ct)
    {
        await db.SaveChangesAsync(ct);
    }

    // ── Query / read-only methods ────────────────────────────────────

    /// <inheritdoc />
    public async Task<(List<RentalProcessInstance> Items, int Total)> ListAsync(
        RentalListQueryDto query,
        RentalAccessFilter accessFilter,
        CancellationToken ct)
    {
        var q = db.RentalProcessInstances
            .Include(p => p.Rental)
            .AsQueryable();

        q = ApplyAccessScope(q, accessFilter);

        if (query.Stages is { Count: > 0 })
            q = q.Where(p => query.Stages.Contains(p.CurrentStage));

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            q = q.Where(p =>
                (p.Rental != null && p.Rental.CustomerName != null && p.Rental.CustomerName.ToLower().Contains(term)) ||
                (p.Rental != null && p.Rental.CustomerEmail != null && p.Rental.CustomerEmail.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(query.OwnerKcId))
            q = q.Where(p => p.Rental != null && p.Rental.CustomerKcId == query.OwnerKcId);

        if (query.GroupGuid is not null)
            q = q.Where(p => p.Rental != null && p.Rental.GroupGuid == query.GroupGuid);

        if (query.CreatedAfter is not null)
            q = q.Where(p => p.CreatedAt >= query.CreatedAfter.Value);

        if (query.CreatedBefore is not null)
            q = q.Where(p => p.CreatedAt < query.CreatedBefore.Value);

        var total = await q.CountAsync(ct);

        q = query.SortBy switch
        {
            RentalSortField.CreatedAt => query.Ascending
                ? q.OrderBy(p => p.CreatedAt)
                : q.OrderByDescending(p => p.CreatedAt),
            RentalSortField.Stage => query.Ascending
                ? q.OrderBy(p => p.CurrentStage)
                : q.OrderByDescending(p => p.CurrentStage),
            RentalSortField.CustomerName => query.Ascending
                ? q.OrderBy(p => p.Rental != null ? p.Rental.CustomerName : null)
                : q.OrderByDescending(p => p.Rental != null ? p.Rental.CustomerName : null),
            _ => query.Ascending
                ? q.OrderBy(p => p.UpdatedAt)
                : q.OrderByDescending(p => p.UpdatedAt)
        };

        var items = await q
            .Skip(query.Offset)
            .Take(query.Limit)
            .ToListAsync(ct);

        return (items, total);
    }

    /// <inheritdoc />
    public async Task<Dictionary<RentalStage, int>> CountByStageAsync(
        RentalAccessFilter accessFilter,
        CancellationToken ct)
    {
        return await ApplyAccessScope(db.RentalProcessInstances.AsQueryable(), accessFilter)
            .GroupBy(p => p.CurrentStage)
            .Select(g => new { Stage = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Stage, x => x.Count, ct);
    }

    /// <inheritdoc />
    public async Task<int> CountDamageReportsAsync(RentalAccessFilter accessFilter, CancellationToken ct)
    {
        var processIds = ApplyAccessScope(db.RentalProcessInstances.AsQueryable(), accessFilter)
            .Select(p => p.Id);

        return await db.RentalDamageReports.CountAsync(r => processIds.Contains(r.ProcessInstanceId), ct);
    }

    /// <inheritdoc />
    public async Task<int> CountExtensionsAsync(RentalAccessFilter accessFilter, CancellationToken ct)
    {
        var processIds = ApplyAccessScope(db.RentalProcessInstances.AsQueryable(), accessFilter)
            .Select(p => p.Id);

        return await db.RentalExtensions.CountAsync(e => processIds.Contains(e.ProcessInstanceId), ct);
    }

    /// <inheritdoc />
    public async Task<int> CountPendingExtensionsAsync(RentalAccessFilter accessFilter, CancellationToken ct)
    {
        var processIds = ApplyAccessScope(db.RentalProcessInstances.AsQueryable(), accessFilter)
            .Select(p => p.Id);

        return await db.RentalExtensions.CountAsync(
            e => e.IsApproved == null && processIds.Contains(e.ProcessInstanceId),
            ct);
    }

    /// <inheritdoc />
    public async Task<int> CountActionLogsAsync(RentalAccessFilter accessFilter, CancellationToken ct)
    {
        var processIds = ApplyAccessScope(db.RentalProcessInstances.AsQueryable(), accessFilter)
            .Select(p => p.Id);

        return await db.RentalActionLogs.CountAsync(l => processIds.Contains(l.ProcessInstanceId), ct);
    }

    /// <inheritdoc />
    public async Task<int> CountProcessesCreatedSinceAsync(
        Instant since,
        RentalAccessFilter accessFilter,
        CancellationToken ct)
        => await ApplyAccessScope(db.RentalProcessInstances.AsQueryable(), accessFilter)
            .CountAsync(p => p.CreatedAt >= since, ct);

    /// <inheritdoc />
    public async Task<int> CountActionLogsSinceAsync(
        Instant since,
        RentalAccessFilter accessFilter,
        CancellationToken ct)
    {
        var processIds = ApplyAccessScope(db.RentalProcessInstances.AsQueryable(), accessFilter)
            .Select(p => p.Id);

        return await db.RentalActionLogs.CountAsync(
            l => l.PerformedAt >= since && processIds.Contains(l.ProcessInstanceId),
            ct);
    }

    /// <inheritdoc />
    public async Task<int> CountDamageReportsSinceAsync(
        Instant since,
        RentalAccessFilter accessFilter,
        CancellationToken ct)
    {
        var processIds = ApplyAccessScope(db.RentalProcessInstances.AsQueryable(), accessFilter)
            .Select(p => p.Id);

        return await db.RentalDamageReports.CountAsync(
            r => r.ReportedAt >= since && processIds.Contains(r.ProcessInstanceId),
            ct);
    }

    /// <inheritdoc />
    public async Task<int> CountProcessesReachedStageSinceAsync(
        RentalStage stage,
        Instant since,
        RentalAccessFilter accessFilter,
        CancellationToken ct)
    {
        var processIds = ApplyAccessScope(db.RentalProcessInstances.AsQueryable(), accessFilter)
            .Select(p => p.Id);

        return await db.RentalActionLogs.CountAsync(
            l => l.StageAfter == stage
                 && l.Success
                 && l.PerformedAt >= since
                 && processIds.Contains(l.ProcessInstanceId),
            ct);
    }

    /// <inheritdoc />
    public async Task<(List<RentalActionLog> Items, int Total)> GetActionLogsByProcessGuidAsync(
        Guid processGuid, int limit, int offset, CancellationToken ct)
    {
        var query = db.RentalActionLogs
            .Where(l => l.ProcessInstance.Guid == processGuid)
            .OrderByDescending(l => l.PerformedAt);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip(offset)
            .Take(limit)
            .ToListAsync(ct);

        return (items, total);
    }
}
