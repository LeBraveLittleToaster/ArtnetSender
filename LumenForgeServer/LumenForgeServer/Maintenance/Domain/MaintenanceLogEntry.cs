using NodaTime;

namespace LumenForgeServer.Maintenance.Domain;

/// <summary>
/// Immutable status-change log entry for a maintenance task.
/// </summary>
public class MaintenanceLogEntry
{
    public long Id { get; set; }
    public Guid Guid { get; set; }

    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;

    public long MaintenanceTaskId { get; set; }
    public MaintenanceTask MaintenanceTask { get; set; } = null!;

    public MaintenanceStatus StatusBefore { get; set; }
    public MaintenanceStatus StatusAfter { get; set; }

    public Instant CreatedAt { get; set; }
}
