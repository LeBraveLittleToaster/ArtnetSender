using LumenForgeServer.Common;
using LumenForgeServer.Maintenance.Domain;
using NodaTime;

namespace LumenForgeServer.Rentals.Domain;

/// <summary>
/// Records damage to a specific device returned as part of a rental line item.
/// Capturing rental-business concerns (severity, cost estimates) while delegating
/// the repair workflow to a linked <see cref="MaintenanceJob"/>.
/// </summary>
public class RentalItemDamageReport
{
    public long Id { get; set; }
    public Guid Uuid { get; set; }

    public long RentalItemId { get; set; }
    public RentalItem RentalItem { get; set; } = null!;

    // The checklist item that originally flagged the damage (optional)
    public long? ChecklistItemId { get; set; }
    public ChecklistItem? ChecklistItem { get; set; }

    // Maintenance job spawned to handle the repair (optional until staff creates one)
    public long? MaintenanceJobId { get; set; }
    public MaintenanceJob? MaintenanceJob { get; set; }

    public DamageSeverity Severity { get; set; }
    public string Description { get; set; } = null!;
    public string? PhotoUrl { get; set; }

    // Rental-billing cost tracking (separate from the maintenance job's repair details)
    public decimal? EstimatedRepairCost { get; set; }
    public decimal? ActualRepairCost { get; set; }

    // Keycloak user id
    public string ReportedByUserId { get; set; } = null!;
    public Instant ReportedAt { get; set; }

    public bool IsResolved { get; set; }
    public Instant? ResolvedAt { get; set; }
    public string? ResolvedByUserId { get; set; }
    public string? ResolutionNotes { get; set; }

    public Instant CreatedAt { get; set; }
    public Instant UpdatedAt { get; set; }
}
