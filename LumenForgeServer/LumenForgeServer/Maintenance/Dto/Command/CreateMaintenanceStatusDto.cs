using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Maintenance.Dto.Command;

/// <summary>
/// Payload for creating a maintenance backlog status.
/// </summary>
public sealed record CreateMaintenanceStatusDto
{
    [Required]
    [StringLength(128, MinimumLength = 1)]
    [RegularExpression(@".*\S.*")]
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [StringLength(2000)]
    [JsonPropertyName("description")]
    public string? Description { get; init; }
}
