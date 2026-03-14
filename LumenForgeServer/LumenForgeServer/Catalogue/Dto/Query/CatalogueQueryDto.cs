using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Catalogue.Dto.Query;

/// <summary>
/// Paging and filtering parameters for catalogue list endpoints.
/// </summary>
public sealed record CatalogueQueryDto
{
    [Range(1, 200)]
    [JsonPropertyName("limit")]
    public int Limit { get; init; } = 50;

    [Range(0, int.MaxValue)]
    [JsonPropertyName("offset")]
    public int Offset { get; init; } = 0;

    [StringLength(128)]
    [JsonPropertyName("search")]
    public string? Search { get; init; }

    [JsonPropertyName("published_only")]
    public bool PublishedOnly { get; init; } = true;
}