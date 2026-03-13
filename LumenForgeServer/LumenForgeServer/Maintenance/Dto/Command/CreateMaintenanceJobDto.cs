using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Maintenance.Dto.Command;

/// <summary>
/// Payload for creating a maintenance job.
/// </summary>
public sealed record CreateMaintenanceJobDto
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

    [Required]
    [MinLength(1)]
    [JsonPropertyName("device_guids")]
    public required IReadOnlyList<Guid> DeviceGuids { get; init; }

    [JsonPropertyName("related_rental_uuid")]
    public Guid? RelatedRentalUuid { get; init; }

    [JsonPropertyName("related_job_guids")]
    public IReadOnlyList<Guid> RelatedJobGuids { get; init; } = [];

    [JsonPropertyName("tasks")]
    public IReadOnlyList<CreateMaintenanceTaskDto> Tasks { get; init; } = [];
}
