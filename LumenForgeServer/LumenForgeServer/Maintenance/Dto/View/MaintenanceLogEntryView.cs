using LumenForgeServer.Maintenance.Domain;
using NodaTime;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Maintenance.Dto.View;

/// <summary>
/// Read model for task status-change log entries.
/// </summary>
public sealed record MaintenanceLogEntryView
{
    [JsonPropertyName("guid")]
    public Guid Guid { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("status_before")]
    public MaintenanceStatus StatusBefore { get; init; }

    [JsonPropertyName("status_after")]
    public MaintenanceStatus StatusAfter { get; init; }

    [JsonPropertyName("created_at")]
    public Instant CreatedAt { get; init; }

    public static MaintenanceLogEntryView FromEntity(MaintenanceLogEntry e) => new()
    {
        Guid = e.Guid,
        Name = e.Name,
        Description = e.Description,
        StatusBefore = e.StatusBefore,
        StatusAfter = e.StatusAfter,
        CreatedAt = e.CreatedAt,
    };
}
