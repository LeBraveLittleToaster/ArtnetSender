using LumenForgeServer.Billing.Domain;
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

    public RentalRequest Request { get; set; } = new();
    public RentalSchedule Schedule { get; set; } = new();
    public RentalAssignment Assignment { get; set; } = new();
    public RentalScrap Scrap { get; set; } = new();

    public Instant CreatedAt { get; set; }
    public Instant? CompletedAt { get; set; }
    public Instant? InvoicedAt { get; set; }
    public Instant? PaidAt { get; set; }
    public Instant? ReportedAt { get; set; }
    public Instant UpdatedAt { get; set; }

    public List<RentalItem> Items { get; set; } = new();
    public List<Checklist> Checklists { get; set; } = new();
    public List<Invoice> Invoices { get; set; } = new();
    public List<RentalEvent> Events { get; set; } = new();
    public List<RentalExtension> Extensions { get; set; } = new();

    public RentalReport RentalReport { get; set; } = null!;
}
