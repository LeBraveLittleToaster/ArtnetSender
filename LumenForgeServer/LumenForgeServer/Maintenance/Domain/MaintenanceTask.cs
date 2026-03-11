using LumenForgeServer.Inventory.Domain;
using NodaTime;

namespace LumenForgeServer.Maintenance.Domain;

/// <summary>
/// Represents a maintenance action item within a maintenance job.
/// </summary>
public class MaintenanceTask
{
    public long Id { get; set; }
    public Guid Guid { get; set; }

    public long MaintenanceJobId { get; set; }
    public MaintenanceJob MaintenanceJob { get; set; } = null!;

    public MaintenanceStatus Status { get; set; }
    public string Description { get; set; } = null!;

    public List<MaintenanceLogEntry> Log { get; set; } = [];
    public List<Device> AffectedDevices { get; set; } = [];

    public string? AssignedToUserKcId { get; set; }

    public Instant CreatedAt { get; set; }
    public Instant UpdatedAt { get; set; }
    public Instant? ResolvedAt { get; set; }
}
