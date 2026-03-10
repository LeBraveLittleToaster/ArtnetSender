using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Maintenance.Dto.Query;

/// <summary>
/// Paging and search parameters for maintenance list endpoints.
/// </summary>
public sealed record MaintenanceQueryDto
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

    /// <summary>
    /// Optional filter by status UUID.
    /// </summary>
    [JsonPropertyName("status_uuid")]
    public Guid? StatusUuid { get; init; }

    /// <summary>
    /// When true, only returns unresolved entries.
    /// </summary>
    [JsonPropertyName("unresolved_only")]
    public bool UnresolvedOnly { get; init; } = false;
}
