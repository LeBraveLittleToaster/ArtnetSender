using LumenForgeServer.Common;
using LumenForgeServer.Common.Database;
using LumenForgeServer.Inventory.Domain;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Domain.Actions;
using LumenForgeServer.Rentals.Dto.Query;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace LumenForgeServer.Rentals.Persistence;

/// <summary>
/// EF Core-backed repository for rental entities.
/// </summary>
public sealed class RentalRepository(AppDbContext db) : IRentalRepository
{
    public Task AddRentalAsync(Rental rental, CancellationToken ct)
        => db.Rentals.AddAsync(rental, ct).AsTask();

    public Task<Rental?> GetRentalByGuidAsync(Guid rentalGuid, RentalInclude include, CancellationToken ct)
        => BuildRentalQuery(include).SingleOrDefaultAsync(r => r.Uuid == rentalGuid, ct);

    public async Task<(IReadOnlyList<Rental> items, long total)> ListRentalsAsync(
        string? search,
        string? customerUserId,
        RentalPriority? priority,
        int limit,
        int offset,
        RentalInclude include,
        CancellationToken ct)
    {
        var query = BuildRentalQuery(include).AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(r =>
                (r.Request.Title != null && r.Request.Title.Contains(search)) ||
                (r.Request.Description != null && r.Request.Description.Contains(search)) ||
                (r.Request.EventName != null && r.Request.EventName.Contains(search)) ||
                r.CustomerUserId.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(customerUserId))
        {
            query = query.Where(r => r.CustomerUserId == customerUserId);
        }

        if (priority.HasValue)
        {
            query = query.Where(r => r.Request.Priority == priority.Value);
        }

        var total = await query.LongCountAsync(ct);
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task DeleteRentalAsync(Rental rental, CancellationToken ct)
    {
        db.Rentals.Remove(rental);
        return Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // Checklists
    // -------------------------------------------------------------------------

    public Task AddChecklistAsync(Checklist checklist, CancellationToken ct)
        => db.Checklists.AddAsync(checklist, ct).AsTask();

    public Task<bool> RentalExistsByGuidAsync(Guid rentalGuid, CancellationToken ct)
        => db.Rentals.AnyAsync(r => r.Uuid == rentalGuid, ct);

    public Task<Checklist?> GetChecklistByGuidAsync(Guid rentalGuid, Guid checklistGuid, CancellationToken ct)
        => db.Checklists
            .Include(c => c.Items).ThenInclude(ci => ci.RentalItem)
            .Include(c => c.SourceChecklist)
            .AsSplitQuery()
            .SingleOrDefaultAsync(c => c.Uuid == checklistGuid && c.Rental.Uuid == rentalGuid, ct);

    public async Task<(IReadOnlyList<Checklist> items, long total)> ListChecklistsForRentalAsync(Guid rentalGuid, int limit, int offset, CancellationToken ct)
    {
        var query = db.Checklists
            .Include(c => c.Items).ThenInclude(ci => ci.RentalItem)
            .Include(c => c.SourceChecklist)
            .Where(c => c.Rental.Uuid == rentalGuid)
            .AsSplitQuery();

        var total = await query.LongCountAsync(ct);
        var items = await query
            .OrderBy(c => c.GeneratedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task<ChecklistItem?> GetChecklistItemByGuidAsync(
        Guid rentalGuid, Guid checklistGuid, Guid itemGuid, CancellationToken ct)
        => db.ChecklistItems
            .Include(ci => ci.Checklist)
            .Include(ci => ci.RentalItem)
            .SingleOrDefaultAsync(ci =>
                ci.Uuid == itemGuid &&
                ci.Checklist.Uuid == checklistGuid &&
                ci.Checklist.Rental.Uuid == rentalGuid, ct);

    public Task<ChecklistItem?> GetChecklistItemByDeviceGuidAsync(
        Guid rentalGuid, Guid checklistGuid, Guid deviceGuid, CancellationToken ct)
        => db.ChecklistItems
            .Include(ci => ci.Checklist)
            .Include(ci => ci.RentalItem)
            .SingleOrDefaultAsync(ci =>
                ci.Checklist.Uuid == checklistGuid &&
                ci.Checklist.Rental.Uuid == rentalGuid &&
                ci.RentalItem.StockBindings.Any(sb => sb.Device.Guid == deviceGuid), ct);

    public async Task<(IReadOnlyList<StockBinding> items, long total)> ListConflictingBindingsAsync(
        long deviceId,
        Instant start,
        Instant end,
        BindingType bindingType,
        int limit,
        int offset,
        CancellationToken ct)
    {
        var query = db.StockBindings
            .Include(sb => sb.Device)
            .Where(sb =>
                sb.DeviceId == deviceId &&
                sb.BindingType == bindingType &&
                sb.Start < end &&
                sb.End > start);

        var total = await query.LongCountAsync(ct);
        var items = await query
            .AsNoTracking()
            .OrderBy(sb => sb.Start)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task SaveChangesAsync(CancellationToken ct)
        => db.SaveChangesAsync(ct);

    // -------------------------------------------------------------------------
    // Actions
    // -------------------------------------------------------------------------

    public async Task<(IReadOnlyList<RentalAction> items, long total)> ListActionsForRentalAsync(
        Guid rentalGuid, int limit, int offset, CancellationToken ct)
    {
        var query = db.RentalActions
            .Where(a => a.Rental.Uuid == rentalGuid);

        var total = await query.LongCountAsync(ct);
        var items = await query
            .AsNoTracking()
            .OrderByDescending(a => a.ExecutedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(ct);

        return (items, total);
    }

    private IQueryable<Rental> BuildRentalQuery(RentalInclude include)
    {
        var query = db.Rentals.AsQueryable();

        if (include.HasFlag(RentalInclude.Items))
        {
            query = query
                .Include(r => r.Items)
                    .ThenInclude(i => i.StockBindings)
                        .ThenInclude(sb => sb.Device);
        }

        if (include.HasFlag(RentalInclude.Checklists))
        {
            query = query
                .Include(r => r.Checklists)
                    .ThenInclude(c => c.Items);
        }

        if (include.HasFlag(RentalInclude.Invoices))
        {
            query = query.Include(r => r.Invoices);
        }

        if (include.HasFlag(RentalInclude.Events))
        {
            query = query.Include(r => r.Events);
        }

        if (include.HasFlag(RentalInclude.Extensions))
        {
            query = query.Include(r => r.Extensions);
        }

        if (include.HasFlag(RentalInclude.Report))
        {
            query = query.Include(r => r.RentalReport);
        }

        if (include.HasFlag(RentalInclude.Actions))
        {
            query = query.Include(r => r.Actions);
        }

        return query.AsSplitQuery();
    }
}
