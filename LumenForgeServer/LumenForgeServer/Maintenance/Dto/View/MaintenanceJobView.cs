using LumenForgeServer.Maintenance.Domain;
using NodaTime;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Maintenance.Dto.View;

/// <summary>
/// Read model for maintenance jobs.
/// </summary>
public sealed record MaintenanceJobView
{
    [JsonPropertyName("guid")]
    public Guid Guid { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("status")]
    public MaintenanceStatus Status { get; init; }

    [JsonPropertyName("created_by_user_kc_id")]
    public required string CreatedByUserKcId { get; init; }

    [JsonPropertyName("affected_device_guids")]
    public IReadOnlyList<Guid> AffectedDeviceGuids { get; init; } = [];

    [JsonPropertyName("related_job_guids")]
    public IReadOnlyList<Guid> RelatedJobGuids { get; init; } = [];

    [JsonPropertyName("related_rental_uuid")]
    public Guid? RelatedRentalUuid { get; init; }

    [JsonPropertyName("tasks")]
    public IReadOnlyList<MaintenanceTaskView> Tasks { get; init; } = [];

    [JsonPropertyName("reported_at")]
    public Instant ReportedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public Instant UpdatedAt { get; init; }

    [JsonPropertyName("resolved_at")]
    public Instant? ResolvedAt { get; init; }

    public static MaintenanceJobView FromEntity(MaintenanceJob e) => new()
    {
        Guid = e.Guid,
        Name = e.Name,
        Description = e.Description,
        Status = e.Status,
        CreatedByUserKcId = e.CreatedByUserKcId,
        AffectedDeviceGuids = e.AffectedDevices.Select(d => d.Guid).ToList(),
        RelatedJobGuids = e.RelatedJobs.Select(r => r.Guid).ToList(),
        RelatedRentalUuid = e.RelatedToRental?.Uuid,
        Tasks = e.Tasks.OrderBy(t => t.CreatedAt).Select(MaintenanceTaskView.FromEntity).ToList(),
        ReportedAt = e.ReportedAt,
        UpdatedAt = e.UpdatedAt,
        ResolvedAt = e.ResolvedAt,
    };
}
