using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Catalogue.Dto.Command;

/// <summary>
/// Payload for creating a catalogue item.
/// </summary>
public sealed record CreateCatalogueItemDto
{
    /// <summary>GUID of the inventory device to publish in the catalogue.</summary>
    [JsonPropertyName("device_guid")]
    public required Guid DeviceGuid { get; init; }

    /// <summary>Customer-facing display name.</summary>
    [Required]
    [StringLength(256, MinimumLength = 1)]
    [RegularExpression(@".*\S.*")]
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>Customer-facing description.</summary>
    [Required]
    [StringLength(4000, MinimumLength = 1)]
    [RegularExpression(@".*\S.*")]
    [JsonPropertyName("description")]
    public required string Description { get; init; }

    /// <summary>Optional photo URL for the catalogue listing.</summary>
    [StringLength(2000)]
    [Url]
    [JsonPropertyName("photo_url")]
    public string? PhotoUrl { get; init; }

    /// <summary>Whether the item is visible to non-admin users.</summary>
    [JsonPropertyName("is_published")]
    public bool IsPublished { get; init; }

    /// <summary>Display order for the catalogue (lower values appear first).</summary>
    [Range(0, int.MaxValue)]
    [JsonPropertyName("sort_order")]
    public int SortOrder { get; init; }
}