using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Maintenance.Dto.Query;

/// <summary>
/// Query parameters for listing maintenance tasks of a job.
/// </summary>
public sealed record MaintenanceTaskQueryDto
{
    [Range(1, 200)]
    [JsonPropertyName("limit")]
    public int Limit { get; init; } = 50;

    [Range(0, int.MaxValue)]
    [JsonPropertyName("offset")]
    public int Offset { get; init; } = 0;
}
