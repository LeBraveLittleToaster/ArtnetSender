using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using LumenForgeServer.Rentals.Domain;
using NodaTime;

namespace LumenForgeServer.Rentals.Dto.Query;

/// <summary>
/// Paging, search, sorting, and filtering parameters for rental process list endpoints.
/// </summary>
public sealed record RentalListQueryDto
{
    /// <summary>Maximum number of records to return.</summary>
    [Range(1, 200)]
    [JsonPropertyName("limit")]
    public int Limit { get; init; } = 50;

    /// <summary>Number of records to skip.</summary>
    [Range(0, int.MaxValue)]
    [JsonPropertyName("offset")]
    public int Offset { get; init; } = 0;

    /// <summary>Optional search term (matches customer name or email).</summary>
    [StringLength(128)]
    [JsonPropertyName("search")]
    public string? Search { get; init; }

    /// <summary>Filter by one or more stages. Empty means all stages.</summary>
    [JsonPropertyName("stages")]
    public List<RentalStage>? Stages { get; init; }

    /// <summary>Field to sort by. Defaults to <see cref="RentalSortField.UpdatedAt"/>.</summary>
    [JsonPropertyName("sortBy")]
    public RentalSortField SortBy { get; init; } = RentalSortField.UpdatedAt;

    /// <summary>Sort direction. <c>true</c> for ascending, <c>false</c> (default) for descending.</summary>
    [JsonPropertyName("ascending")]
    public bool Ascending { get; init; } = false;

    /// <summary>Only return processes created on or after this instant.</summary>
    [JsonPropertyName("createdAfter")]
    public Instant? CreatedAfter { get; init; }

    /// <summary>Only return processes created before this instant.</summary>
    [JsonPropertyName("createdBefore")]
    public Instant? CreatedBefore { get; init; }

    /// <summary>Filter by the Keycloak subject id of the process creator.</summary>
    [JsonPropertyName("ownerKcId")]
    public string? OwnerKcId { get; init; }
}
