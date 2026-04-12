using LumenForgeServer.Maintenance.Domain;
using NodaTime;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Maintenance.Dto.View;

/// <summary>
/// Read model for maintenance tasks.
/// </summary>
public sealed record MaintenanceTaskView
{
    /// <summary>Unique task identifier.</summary>
    [JsonPropertyName("guid")]
    public Guid Guid { get; init; }

    /// <summary>Task description.</summary>
    [JsonPropertyName("description")]
    public required string Description { get; init; }

    /// <summary>Current lifecycle status.</summary>
    [JsonPropertyName("status")]
    public MaintenanceStatus Status { get; init; }

    /// <summary>Keycloak subject identifier of the assigned user, or null if unassigned.</summary>
    [JsonPropertyName("assigned_to_user_kc_id")]
    public string? AssignedToUserKcId { get; init; }

    /// <summary>GUIDs of devices affected by this task.</summary>
    [JsonPropertyName("affected_device_guids")]
    public IReadOnlyList<Guid> AffectedDeviceGuids { get; init; } = [];

    /// <summary>Status-change log entries (included when requested via <c>include=Logs</c>).</summary>
    [JsonPropertyName("log")]
    public IReadOnlyList<MaintenanceLogEntryView> Log { get; init; } = [];

    /// <summary>Timestamp when the task was created.</summary>
    [JsonPropertyName("created_at")]
    public Instant CreatedAt { get; init; }

    /// <summary>Timestamp when the task was last updated.</summary>
    [JsonPropertyName("updated_at")]
    public Instant UpdatedAt { get; init; }

    /// <summary>Timestamp when the task was resolved, or null if still open.</summary>
    [JsonPropertyName("resolved_at")]
    public Instant? ResolvedAt { get; init; }

    /// <summary>
    /// Executes the from entity operation.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="e">Numeric input used by this operation.</param>
    /// <returns>The operation result.</returns>
    public static MaintenanceTaskView FromEntity(MaintenanceTask e) => new()
    {
        Guid = e.Guid,
        Description = e.Description,
        Status = e.Status,
        AssignedToUserKcId = e.AssignedToUserKcId,
        AffectedDeviceGuids = e.AffectedDevices.Select(d => d.Guid).ToList(),
        Log = e.Log.OrderBy(l => l.CreatedAt).Select(MaintenanceLogEntryView.FromEntity).ToList(),
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
        ResolvedAt = e.ResolvedAt,
    };
}
