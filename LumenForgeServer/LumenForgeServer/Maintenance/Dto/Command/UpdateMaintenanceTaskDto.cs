using LumenForgeServer.Maintenance.Domain;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Maintenance.Dto.Command;

/// <summary>
/// Payload for partially updating a maintenance task.
/// </summary>
public sealed record UpdateMaintenanceTaskDto
{
    /// <summary>Updated task description.</summary>
    [StringLength(4000, MinimumLength = 1)]
    [RegularExpression(@".*\S.*")]
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>New lifecycle status for the task.</summary>
    [JsonPropertyName("status")]
    public MaintenanceStatus? Status { get; init; }

    /// <summary>Keycloak subject identifier of the newly assigned user, or null to clear.</summary>
    [JsonPropertyName("assigned_to_user_kc_id")]
    public string? AssignedToUserKcId { get; init; }
}
