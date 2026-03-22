using LumenForgeServer.Maintenance.Domain;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Maintenance.Dto.Command;

/// <summary>
/// Payload for creating a task in a maintenance job.
/// </summary>
public sealed record CreateMaintenanceTaskDto
{
    /// <summary>Detailed description of the task work.</summary>
    [Required]
    [StringLength(4000, MinimumLength = 1)]
    [RegularExpression(@".*\S.*")]
    [JsonPropertyName("description")]
    public required string Description { get; init; }

    /// <summary>Initial status (defaults to Reported).</summary>
    [JsonPropertyName("status")]
    public MaintenanceStatus Status { get; init; } = MaintenanceStatus.Reported;

    /// <summary>Optional Keycloak subject identifier of the user assigned to this task.</summary>
    [JsonPropertyName("assigned_to_user_kc_id")]
    public string? AssignedToUserKcId { get; init; }

    /// <summary>Optional GUIDs of devices affected by this specific task.</summary>
    [JsonPropertyName("affected_device_guids")]
    public IReadOnlyList<Guid> AffectedDeviceGuids { get; init; } = [];
}
