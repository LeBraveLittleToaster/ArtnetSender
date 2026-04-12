using LumenForgeServer.Maintenance.Domain;
using NodaTime;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Maintenance.Dto.View;

/// <summary>
/// Read model for task status-change log entries.
/// </summary>
public sealed record MaintenanceLogEntryView
{
    /// <summary>Unique log entry identifier.</summary>
    [JsonPropertyName("guid")]
    public Guid Guid { get; init; }

    /// <summary>Short title of the log entry.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>Detailed description of what happened.</summary>
    [JsonPropertyName("description")]
    public required string Description { get; init; }

    /// <summary>Task status before this log entry was recorded.</summary>
    [JsonPropertyName("status_before")]
    public MaintenanceStatus StatusBefore { get; init; }

    /// <summary>Task status after this log entry was recorded.</summary>
    [JsonPropertyName("status_after")]
    public MaintenanceStatus StatusAfter { get; init; }

    /// <summary>Timestamp when the log entry was created.</summary>
    [JsonPropertyName("created_at")]
    public Instant CreatedAt { get; init; }

    /// <summary>
    /// Executes the from entity operation.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="e">Numeric input used by this operation.</param>
    /// <returns>The operation result.</returns>
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
