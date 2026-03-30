using LumenForgeServer.Maintenance.Domain;
using NodaTime;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Maintenance.Dto.View;

/// <summary>
/// Read model for maintenance jobs.
/// </summary>
public sealed record MaintenanceJobView
{
    /// <summary>Unique job identifier.</summary>
    [JsonPropertyName("guid")]
    public Guid Guid { get; init; }

    /// <summary>Job title.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>Detailed job description.</summary>
    [JsonPropertyName("description")]
    public required string Description { get; init; }

    /// <summary>Current lifecycle status.</summary>
    [JsonPropertyName("status")]
    public MaintenanceStatus Status { get; init; }

    /// <summary>Keycloak subject identifier of the user who created the job.</summary>
    [JsonPropertyName("created_by_user_kc_id")]
    public required string CreatedByUserKcId { get; init; }

    /// <summary>GUIDs of devices affected by this job.</summary>
    [JsonPropertyName("affected_device_guids")]
    public IReadOnlyList<Guid> AffectedDeviceGuids { get; init; } = [];

    /// <summary>GUIDs of related maintenance jobs.</summary>
    [JsonPropertyName("related_job_guids")]
    public IReadOnlyList<Guid> RelatedJobGuids { get; init; } = [];

    /// <summary>Optional UUID of a linked rental.</summary>
    [JsonPropertyName("related_rental_uuid")]
    public Guid? RelatedRentalUuid { get; init; }

    /// <summary>Tasks within this job (included when requested via <c>include=Tasks</c>).</summary>
    [JsonPropertyName("tasks")]
    public IReadOnlyList<MaintenanceTaskView> Tasks { get; init; } = [];

    /// <summary>Timestamp when the job was reported / created.</summary>
    [JsonPropertyName("reported_at")]
    public Instant ReportedAt { get; init; }

    /// <summary>Timestamp when the job was last updated.</summary>
    [JsonPropertyName("updated_at")]
    public Instant UpdatedAt { get; init; }

    /// <summary>Timestamp when the job was resolved, or null if still open.</summary>
    [JsonPropertyName("resolved_at")]
    public Instant? ResolvedAt { get; init; }

    /// <summary>
    /// Executes the from entity operation.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="e">Numeric input used by this operation.</param>
    /// <returns>The operation result.</returns>
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
