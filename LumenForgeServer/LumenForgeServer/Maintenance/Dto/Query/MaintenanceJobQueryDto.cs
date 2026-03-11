using LumenForgeServer.Maintenance.Domain;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Maintenance.Dto.Query;

/// <summary>
/// Query parameters for listing maintenance jobs.
/// </summary>
public sealed record MaintenanceJobQueryDto
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

    [JsonPropertyName("status")]
    public MaintenanceStatus? Status { get; init; }

    [JsonPropertyName("unresolved_only")]
    public bool UnresolvedOnly { get; init; } = false;
}
