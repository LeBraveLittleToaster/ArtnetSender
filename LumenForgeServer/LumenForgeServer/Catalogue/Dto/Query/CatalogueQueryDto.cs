using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Catalogue.Dto.Query;

/// <summary>
/// Paging and filtering parameters for catalogue list endpoints.
/// </summary>
public sealed record CatalogueQueryDto
{
    /// <summary>Maximum number of records to return (1–200, default 50).</summary>
    [Range(1, 200)]
    [JsonPropertyName("limit")]
    public int Limit { get; init; } = 50;

    /// <summary>Number of records to skip (default 0).</summary>
    [Range(0, int.MaxValue)]
    [JsonPropertyName("offset")]
    public int Offset { get; init; } = 0;

    /// <summary>Optional search term (matches item name or description).</summary>
    [StringLength(128)]
    [JsonPropertyName("search")]
    public string? Search { get; init; }

    /// <summary>When true (default), only published items are returned.</summary>
    [JsonPropertyName("published_only")]
    public bool PublishedOnly { get; init; } = true;
}