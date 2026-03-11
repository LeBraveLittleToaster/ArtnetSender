using LumenForgeServer.Maintenance.Domain;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Maintenance.Dto.Command;

/// <summary>
/// Payload for partially updating a maintenance task.
/// </summary>
public sealed record UpdateMaintenanceTaskDto
{
    [StringLength(4000, MinimumLength = 1)]
    [RegularExpression(@".*\S.*")]
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("status")]
    public MaintenanceStatus? Status { get; init; }

    [JsonPropertyName("assigned_to_user_kc_id")]
    public string? AssignedToUserKcId { get; init; }
}
