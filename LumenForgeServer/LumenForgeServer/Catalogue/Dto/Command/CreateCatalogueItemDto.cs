using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Catalogue.Dto.Command;

/// <summary>
/// Payload for creating a catalogue item.
/// </summary>
public sealed record CreateCatalogueItemDto
{
    [JsonPropertyName("device_guid")]
    public required Guid DeviceGuid { get; init; }

    [Required]
    [StringLength(256, MinimumLength = 1)]
    [RegularExpression(@".*\S.*")]
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [Required]
    [StringLength(4000, MinimumLength = 1)]
    [RegularExpression(@".*\S.*")]
    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [StringLength(2000)]
    [Url]
    [JsonPropertyName("photo_url")]
    public string? PhotoUrl { get; init; }

    [JsonPropertyName("is_published")]
    public bool IsPublished { get; init; }

    [Range(0, int.MaxValue)]
    [JsonPropertyName("sort_order")]
    public int SortOrder { get; init; }
}