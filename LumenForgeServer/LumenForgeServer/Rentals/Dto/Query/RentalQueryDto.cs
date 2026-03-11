using LumenForgeServer.Common;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Rentals.Dto.Query;

/// <summary>
/// Paging, search, and filter parameters for listing rentals.
/// </summary>
public sealed record RentalQueryDto
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

    [JsonPropertyName("customer_user_id")]
    public string? CustomerUserId { get; init; }

    [JsonPropertyName("priority")]
    public RentalPriority? Priority { get; init; }
}
