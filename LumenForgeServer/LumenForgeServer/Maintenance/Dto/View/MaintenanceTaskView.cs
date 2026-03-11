using LumenForgeServer.Maintenance.Domain;
using NodaTime;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Maintenance.Dto.View;

/// <summary>
/// Read model for maintenance tasks.
/// </summary>
public sealed record MaintenanceTaskView
{
    [JsonPropertyName("guid")]
    public Guid Guid { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("status")]
    public MaintenanceStatus Status { get; init; }

    [JsonPropertyName("assigned_to_user_kc_id")]
    public string? AssignedToUserKcId { get; init; }

    [JsonPropertyName("affected_device_guids")]
    public IReadOnlyList<Guid> AffectedDeviceGuids { get; init; } = [];

    [JsonPropertyName("log")]
    public IReadOnlyList<MaintenanceLogEntryView> Log { get; init; } = [];

    [JsonPropertyName("created_at")]
    public Instant CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public Instant UpdatedAt { get; init; }

    [JsonPropertyName("resolved_at")]
    public Instant? ResolvedAt { get; init; }

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
