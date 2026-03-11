using LumenForgeServer.Maintenance.Domain;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Maintenance.Dto.Command;

/// <summary>
/// Payload for creating an immutable log entry for a maintenance task.
/// </summary>
public sealed record CreateMaintenanceLogEntryDto
{
    [Required]
    [StringLength(256, MinimumLength = 1)]
    [RegularExpression(@".*\S.*")]
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [Required]
    [StringLength(4000, MinimumLength = 1)]
    [RegularExpression(@".*\S.*")]
    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("status_after")]
    public MaintenanceStatus? StatusAfter { get; init; }
}
