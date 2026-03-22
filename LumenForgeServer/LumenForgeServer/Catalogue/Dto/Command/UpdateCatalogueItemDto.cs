using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Catalogue.Dto.Command;

/// <summary>
/// Payload for partially updating a catalogue item.
/// </summary>
public sealed record UpdateCatalogueItemDto
{
    /// <summary>New device GUID to link.</summary>
    [JsonPropertyName("device_guid")]
    public Guid? DeviceGuid { get; init; }

    /// <summary>Updated display name.</summary>
    [StringLength(256, MinimumLength = 1)]
    [RegularExpression(@".*\S.*")]
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>Updated description.</summary>
    [StringLength(4000, MinimumLength = 1)]
    [RegularExpression(@".*\S.*")]
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>Updated photo URL.</summary>
    [StringLength(2000)]
    [Url]
    [JsonPropertyName("photo_url")]
    public string? PhotoUrl { get; init; }

    /// <summary>Updated publication flag.</summary>
    [JsonPropertyName("is_published")]
    public bool? IsPublished { get; init; }

    /// <summary>Updated sort order.</summary>
    [Range(0, int.MaxValue)]
    [JsonPropertyName("sort_order")]
    public int? SortOrder { get; init; }
}