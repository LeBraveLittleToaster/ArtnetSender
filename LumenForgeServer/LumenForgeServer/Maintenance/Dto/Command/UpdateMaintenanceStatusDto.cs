using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Maintenance.Dto.Command;

/// <summary>
/// Payload for partially updating a maintenance backlog status.
/// </summary>
public sealed record UpdateMaintenanceStatusDto
{
    [StringLength(128, MinimumLength = 1)]
    [RegularExpression(@".*\S.*")]
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [StringLength(2000)]
    [JsonPropertyName("description")]
    public string? Description { get; init; }
}
