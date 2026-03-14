using LumenForgeServer.Inventory.Domain;
using NodaTime;

namespace LumenForgeServer.Catalogue.Domain;

/// <summary>
/// Represents a public catalogue entry for a rentable device.
/// </summary>
public class CatalogueItem
{
    public long Id { get; set; }
    public Guid Guid { get; set; }

    public long DeviceId { get; set; }
    public Device Device { get; set; } = null!;

    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string? PhotoUrl { get; set; }
    public bool IsPublished { get; set; }
    public int SortOrder { get; set; }

    public Instant CreatedAt { get; set; }
    public Instant UpdatedAt { get; set; }
}