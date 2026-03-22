using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Maintenance.Dto.Command;

/// <summary>
/// Payload for creating a maintenance job.
/// </summary>
public sealed record CreateMaintenanceJobDto
{
    /// <summary>Short name / title for the job.</summary>
    [Required]
    [StringLength(256, MinimumLength = 1)]
    [RegularExpression(@".*\S.*")]
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>Detailed description of the work required.</summary>
    [Required]
    [StringLength(4000, MinimumLength = 1)]
    [RegularExpression(@".*\S.*")]
    [JsonPropertyName("description")]
    public required string Description { get; init; }

    /// <summary>GUIDs of devices affected by this job (at least one required).</summary>
    [Required]
    [MinLength(1)]
    [JsonPropertyName("device_guids")]
    public required IReadOnlyList<Guid> DeviceGuids { get; init; }

    /// <summary>Optional UUID of a rental linked to this maintenance work.</summary>
    [JsonPropertyName("related_rental_uuid")]
    public Guid? RelatedRentalUuid { get; init; }

    /// <summary>Optional GUIDs of related maintenance jobs.</summary>
    [JsonPropertyName("related_job_guids")]
    public IReadOnlyList<Guid> RelatedJobGuids { get; init; } = [];

    /// <summary>Tasks to create inline together with the job.</summary>
    [JsonPropertyName("tasks")]
    public IReadOnlyList<CreateMaintenanceTaskDto> Tasks { get; init; } = [];
}
