using LumenForgeServer.Maintenance.Domain;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Maintenance.Dto.Command;

/// <summary>
/// Payload for creating an immutable log entry for a maintenance task.
/// </summary>
public sealed record CreateMaintenanceLogEntryDto
{
    /// <summary>Short title for the log entry.</summary>
    [Required]
    [StringLength(256, MinimumLength = 1)]
    [RegularExpression(@".*\S.*")]
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>Detailed description of the status change or work performed.</summary>
    [Required]
    [StringLength(4000, MinimumLength = 1)]
    [RegularExpression(@".*\S.*")]
    [JsonPropertyName("description")]
    public required string Description { get; init; }

    /// <summary>Optional new status to apply to the task after this log entry. If omitted the status stays unchanged.</summary>
    [JsonPropertyName("status_after")]
    public MaintenanceStatus? StatusAfter { get; init; }
}
