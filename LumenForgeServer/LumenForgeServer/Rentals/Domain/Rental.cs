using LumenForgeServer.Billing.Domain;
using LumenForgeServer.Common;
using NodaTime;

namespace LumenForgeServer.Rentals.Domain;

/// <summary>
/// Represents a rental request and lifecycle state for one customer transaction.
/// </summary>
public class Rental
{
    public long Id { get; set; }
    public Guid Uuid { get; set; }

    public long RentalStatusId { get; set; }
    public RentalStatus RentalStatus { get; set; } = null!;

    // Keycloak user id
    public string CustomerUserId { get; set; } = null!;

    public string? RequestTitle { get; set; }
    public string? RequestDescription { get; set; }
    public string? EventName { get; set; }
    public string? CustomerNotes { get; set; }
    public string? DeliveryAddress { get; set; }

    public RentalPriority Priority { get; set; } = RentalPriority.NORMAL;

    public Instant? RequestedAt { get; set; }

    // Planned vs actual pickup and return
    public Instant? PlannedPickupAt { get; set; }
    public Instant? PlannedReturnAt { get; set; }

    public Instant CreatedAt { get; set; }
    public Instant? PickupAt { get; set; }
    public Instant? DropoffAt { get; set; }
    public Instant? CompletedAt { get; set; }
    public Instant? InvoicedAt { get; set; }
    public Instant? PaidAt { get; set; }
    public Instant? ReportedAt { get; set; }

    public string? AssignedByUserId { get; set; }
    public Instant? AssignedAt { get; set; }

    public string? PickupProcessedByUserId { get; set; }
    public string? DropoffProcessedByUserId { get; set; }
    public string? CompletedByUserId { get; set; }

    public bool IsScrapped { get; set; }
    public Instant? ScrappedAt { get; set; }
    public string? ScrappedByUserId { get; set; }

    public Instant UpdatedAt { get; set; }

    public List<RentalItem> Items { get; set; } = new();
    public List<Checklist> Checklists { get; set; } = new();
    public List<Invoice> Invoices { get; set; } = new();
    public List<RentalEvent> Events { get; set; } = new();
    public List<RentalExtension> Extensions { get; set; } = new();

    public RentalReport RentalReport { get; set; } = null!;
}
