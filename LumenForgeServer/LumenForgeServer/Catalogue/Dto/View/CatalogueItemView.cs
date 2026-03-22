using LumenForgeServer.Catalogue.Domain;
using NodaTime;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Catalogue.Dto.View;

/// <summary>
/// Read model for catalogue items.
/// </summary>
public sealed record CatalogueItemView
{
    /// <summary>Unique catalogue item identifier.</summary>
    [JsonPropertyName("guid")]
    public Guid Guid { get; init; }

    /// <summary>GUID of the linked inventory device.</summary>
    [JsonPropertyName("device_guid")]
    public Guid DeviceGuid { get; init; }

    /// <summary>Customer-facing display name.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>Customer-facing description.</summary>
    [JsonPropertyName("description")]
    public required string Description { get; init; }

    /// <summary>Optional photo URL.</summary>
    [JsonPropertyName("photo_url")]
    public string? PhotoUrl { get; init; }

    /// <summary>Whether the item is visible to non-admin users.</summary>
    [JsonPropertyName("is_published")]
    public bool IsPublished { get; init; }

    /// <summary>Display order (lower values appear first).</summary>
    [JsonPropertyName("sort_order")]
    public int SortOrder { get; init; }

    /// <summary>Timestamp when the item was created.</summary>
    [JsonPropertyName("created_at")]
    public Instant CreatedAt { get; init; }

    /// <summary>Timestamp when the item was last updated.</summary>
    [JsonPropertyName("updated_at")]
    public Instant UpdatedAt { get; init; }

    public static CatalogueItemView FromEntity(CatalogueItem item) => new()
    {
        Guid = item.Guid,
        DeviceGuid = item.Device.Guid,
        Name = item.Name,
        Description = item.Description,
        PhotoUrl = item.PhotoUrl,
        IsPublished = item.IsPublished,
        SortOrder = item.SortOrder,
        CreatedAt = item.CreatedAt,
        UpdatedAt = item.UpdatedAt,
    };
}