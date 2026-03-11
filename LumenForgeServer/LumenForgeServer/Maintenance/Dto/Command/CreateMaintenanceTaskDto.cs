using LumenForgeServer.Maintenance.Domain;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Maintenance.Dto.Command;

/// <summary>
/// Payload for creating a task in a maintenance job.
/// </summary>
public sealed record CreateMaintenanceTaskDto
{
    [Required]
    [StringLength(4000, MinimumLength = 1)]
    [RegularExpression(@".*\S.*")]
    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("status")]
    public MaintenanceStatus Status { get; init; } = MaintenanceStatus.Reported;

    [JsonPropertyName("assigned_to_user_kc_id")]
    public string? AssignedToUserKcId { get; init; }

    [JsonPropertyName("affected_device_guids")]
    public IReadOnlyList<Guid> AffectedDeviceGuids { get; init; } = [];
}
