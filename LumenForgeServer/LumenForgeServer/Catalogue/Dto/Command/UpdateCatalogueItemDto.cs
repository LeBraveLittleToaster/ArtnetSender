using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Catalogue.Dto.Command;

/// <summary>
/// Payload for partially updating a catalogue item.
/// </summary>
public sealed record UpdateCatalogueItemDto
{
    [JsonPropertyName("device_guid")]
    public Guid? DeviceGuid { get; init; }

    [StringLength(256, MinimumLength = 1)]
    [RegularExpression(@".*\S.*")]
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [StringLength(4000, MinimumLength = 1)]
    [RegularExpression(@".*\S.*")]
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [StringLength(2000)]
    [Url]
    [JsonPropertyName("photo_url")]
    public string? PhotoUrl { get; init; }

    [JsonPropertyName("is_published")]
    public bool? IsPublished { get; init; }

    [Range(0, int.MaxValue)]
    [JsonPropertyName("sort_order")]
    public int? SortOrder { get; init; }
}