using LumenForgeServer.Catalogue.Domain;
using NodaTime;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Catalogue.Dto.View;

/// <summary>
/// Read model for catalogue items.
/// </summary>
public sealed record CatalogueItemView
{
    [JsonPropertyName("guid")]
    public Guid Guid { get; init; }

    [JsonPropertyName("device_guid")]
    public Guid DeviceGuid { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("photo_url")]
    public string? PhotoUrl { get; init; }

    [JsonPropertyName("is_published")]
    public bool IsPublished { get; init; }

    [JsonPropertyName("sort_order")]
    public int SortOrder { get; init; }

    [JsonPropertyName("created_at")]
    public Instant CreatedAt { get; init; }

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