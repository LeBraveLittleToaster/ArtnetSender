using LumenForgeServer.Catalogue.Domain;
using LumenForgeServer.Common.Database;
using Microsoft.EntityFrameworkCore;

namespace LumenForgeServer.Catalogue.Persistence;

/// <summary>
/// EF Core-backed repository for catalogue entities.
/// </summary>
public sealed class CatalogueRepository(AppDbContext db) : ICatalogueRepository
{
    /// <summary>
    /// Executes the add item async operation.
    /// Core concept: maps application requests to persistence queries and materializes domain data.
    /// </summary>
    /// <remarks>Potential side effects: may execute database writes or update tracked entity state in the current DbContext.</remarks>
    /// <param name="item">Input value used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task AddItemAsync(CatalogueItem item, CancellationToken ct)
        => db.CatalogueItems.AddAsync(item, ct).AsTask();

    /// <summary>
    /// Executes the get item by guid async operation.
    /// Core concept: maps application requests to persistence queries and materializes domain data.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="itemGuid">Unique identifier used to target the requested entity.</param>
    /// <param name="includeUnpublished">Boolean flag controlling the operation behavior.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the CatalogueItem? result.</returns>
    public Task<CatalogueItem?> GetItemByGuidAsync(Guid itemGuid, bool includeUnpublished, CancellationToken ct)
    {
        var query = BuildItemQuery();
        if (!includeUnpublished)
        {
            query = query.Where(i => i.IsPublished);
        }

        return query.SingleOrDefaultAsync(i => i.Guid == itemGuid, ct);
    }

    /// <summary>
    /// Executes the task operation.
    /// Core concept: maps application requests to persistence queries and materializes domain data.
    /// </summary>
    /// <remarks>Potential side effects: may execute database writes or update tracked entity state in the current DbContext.</remarks>
    /// <param name="search">Text input used by this operation.</param>
    /// <param name="limit">Numeric input used by this operation.</param>
    /// <param name="offset">Numeric input used by this operation.</param>
    /// <param name="publishedOnly">Boolean flag controlling the operation behavior.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>The operation result.</returns>
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

    /// <summary>
    /// Executes the delete item async operation.
    /// Core concept: maps application requests to persistence queries and materializes domain data.
    /// </summary>
    /// <remarks>Potential side effects: may execute database writes or update tracked entity state in the current DbContext.</remarks>
    /// <param name="item">Input value used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task DeleteItemAsync(CatalogueItem item, CancellationToken ct)
    {
        db.CatalogueItems.Remove(item);
        return Task.CompletedTask;
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
    /// Executes the build item query operation.
    /// Core concept: maps application requests to persistence queries and materializes domain data.
    /// </summary>
    /// <remarks>Potential side effects: may execute database writes or update tracked entity state in the current DbContext.</remarks>
    /// <returns>The operation result.</returns>
    private IQueryable<CatalogueItem> BuildItemQuery()
        => db.CatalogueItems
            .Include(i => i.Device)
            .AsSplitQuery();
}