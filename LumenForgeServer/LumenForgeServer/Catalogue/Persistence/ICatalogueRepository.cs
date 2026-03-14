using LumenForgeServer.Catalogue.Domain;

namespace LumenForgeServer.Catalogue.Persistence;

/// <summary>
/// Persistence contract for catalogue entities.
/// </summary>
public interface ICatalogueRepository
{
    Task AddItemAsync(CatalogueItem item, CancellationToken ct);
    Task<CatalogueItem?> GetItemByGuidAsync(Guid itemGuid, bool includeUnpublished, CancellationToken ct);
    Task<(IReadOnlyList<CatalogueItem> items, long total)> ListItemsAsync(string? search, int limit, int offset, bool publishedOnly, CancellationToken ct);
    Task DeleteItemAsync(CatalogueItem item, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}