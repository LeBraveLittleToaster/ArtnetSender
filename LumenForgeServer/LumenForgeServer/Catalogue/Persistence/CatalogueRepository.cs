using LumenForgeServer.Catalogue.Domain;
using LumenForgeServer.Common.Database;
using Microsoft.EntityFrameworkCore;

namespace LumenForgeServer.Catalogue.Persistence;

/// <summary>
/// EF Core-backed repository for catalogue entities.
/// </summary>
public sealed class CatalogueRepository(AppDbContext db) : ICatalogueRepository
{
    public Task AddItemAsync(CatalogueItem item, CancellationToken ct)
        => db.CatalogueItems.AddAsync(item, ct).AsTask();

    public Task<CatalogueItem?> GetItemByGuidAsync(Guid itemGuid, bool includeUnpublished, CancellationToken ct)
    {
        var query = BuildItemQuery();
        if (!includeUnpublished)
        {
            query = query.Where(i => i.IsPublished);
        }

        return query.SingleOrDefaultAsync(i => i.Guid == itemGuid, ct);
    }

    public async Task<(IReadOnlyList<CatalogueItem> items, long total)> ListItemsAsync(string? search, int limit, int offset, bool publishedOnly, CancellationToken ct)
    {
        var query = BuildItemQuery().AsNoTracking();

        if (publishedOnly)
        {
            query = query.Where(i => i.IsPublished);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(i =>
                i.Name.Contains(search) ||
                i.Description.Contains(search) ||
                i.Device.SerialNumber.Contains(search) ||
                (i.Device.DeviceName != null && i.Device.DeviceName.Contains(search)) ||
                (i.Device.DeviceDescription != null && i.Device.DeviceDescription.Contains(search)));
        }

        var total = await query.LongCountAsync(ct);
        var items = await query
            .OrderBy(i => i.SortOrder)
            .ThenBy(i => i.Name)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task DeleteItemAsync(CatalogueItem item, CancellationToken ct)
    {
        db.CatalogueItems.Remove(item);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct)
        => db.SaveChangesAsync(ct);

    private IQueryable<CatalogueItem> BuildItemQuery()
        => db.CatalogueItems
            .Include(i => i.Device)
            .AsSplitQuery();
}