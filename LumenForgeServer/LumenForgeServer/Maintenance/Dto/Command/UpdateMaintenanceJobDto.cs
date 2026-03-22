using LumenForgeServer.Maintenance.Domain;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Maintenance.Dto.Command;

/// <summary>
/// Payload for partially updating a maintenance job.
/// </summary>
public sealed record UpdateMaintenanceJobDto
{
    /// <summary>Updated job name.</summary>
    [StringLength(256, MinimumLength = 1)]
    [RegularExpression(@".*\S.*")]
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>Updated job description.</summary>
    [StringLength(4000, MinimumLength = 1)]
    [RegularExpression(@".*\S.*")]
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>New lifecycle status to set on the job.</summary>
    [JsonPropertyName("status")]
    public MaintenanceStatus? Status { get; init; }
}
