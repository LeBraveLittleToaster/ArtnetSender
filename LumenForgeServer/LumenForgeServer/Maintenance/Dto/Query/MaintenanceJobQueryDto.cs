using LumenForgeServer.Maintenance.Domain;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Maintenance.Dto.Query;

/// <summary>
/// Query parameters for listing maintenance jobs.
/// </summary>
public sealed record MaintenanceJobQueryDto
{
    /// <summary>Maximum number of records to return (1–200, default 50).</summary>
    [Range(1, 200)]
    [JsonPropertyName("limit")]
    public int Limit { get; init; } = 50;

    /// <summary>Number of records to skip (default 0).</summary>
    [Range(0, int.MaxValue)]
    [JsonPropertyName("offset")]
    public int Offset { get; init; } = 0;

    /// <summary>Optional search term (matches job name or description).</summary>
    [StringLength(128)]
    [JsonPropertyName("search")]
    public string? Search { get; init; }

    /// <summary>Filter by a specific status. Omit to include all.</summary>
    [JsonPropertyName("status")]
    public MaintenanceStatus? Status { get; init; }

    /// <summary>When true, only unresolved (open) jobs are returned.</summary>
    [JsonPropertyName("unresolved_only")]
    public bool UnresolvedOnly { get; init; } = false;
}
