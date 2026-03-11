using LumenForgeServer.Inventory.Domain;
using LumenForgeServer.Rentals.Domain;
using NodaTime;

namespace LumenForgeServer.Maintenance.Domain;

/// <summary>
/// Represents a maintenance case aggregating tasks and affected devices.
/// </summary>
public class MaintenanceJob
{
    public long Id { get; set; }
    public Guid Guid { get; set; }

    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;

    public MaintenanceStatus Status { get; set; }

    public List<Device> AffectedDevices { get; set; } = [];
    public List<MaintenanceTask> Tasks { get; set; } = [];
    public List<MaintenanceJob> RelatedJobs { get; set; } = [];

    public string CreatedByUserKcId { get; set; } = null!;

    public long? RelatedToRentalId { get; set; }
    public Rental? RelatedToRental { get; set; }

    public Instant ReportedAt { get; set; }
    public Instant UpdatedAt { get; set; }
    public Instant? ResolvedAt { get; set; }
}
