using LumenForgeServer.Common;
using LumenForgeServer.Inventory.Domain;
using NodaTime;

namespace LumenForgeServer.Rentals.Domain;

/// <summary>
/// Represents a requested device line item within a rental,
/// tracking the full lifecycle from request through return.
/// Each physical unit allocated to this line item is represented by a
/// <see cref="StockBinding"/> (type <c>RENTAL_REQUEST</c> on creation,
/// promoted to <c>RENTAL</c> on pickup).
/// </summary>
public class RentalItem
{
    public long Id { get; set; }
    public Guid Uuid { get; set; }

    public long RentalId { get; set; }
    public Rental Rental { get; set; } = null!;

    public RentalItemStatus Status { get; set; } = RentalItemStatus.REQUESTED;

    // Quantities
    public decimal QuantityRequested { get; set; }
    public decimal? QuantityApproved { get; set; }
    public decimal? QuantityPickedUp { get; set; }
    public decimal? QuantityReturned { get; set; }
    public decimal? QuantityDamaged { get; set; }
    public decimal? QuantityLost { get; set; }

    // Approval
    public bool IsApproved { get; set; }
    public Instant? ApprovedAt { get; set; }
    // nullable (customer or provider) - Keycloak user id
    public string? ApprovedByUserId { get; set; }
    public string? RejectionReason { get; set; }

    // Customer-requested window; StockBinding.Start/End is the actual reservation window set by staff
    public Instant? PlannedPickupAt { get; set; }
    public Instant? PlannedReturnAt { get; set; }
    public Instant? ActualPickupAt { get; set; }
    public Instant? ActualReturnAt { get; set; }

    // Staff who processed pickup and return - Keycloak user ids
    public string? PickupProcessedByUserId { get; set; }
    public string? ReturnProcessedByUserId { get; set; }

    // Pricing
    public decimal? DailyRate { get; set; }
    public decimal? DepositAmount { get; set; }

    // Notes
    public string? ConditionNotes { get; set; }
    public string? PickupNotes { get; set; }
    public string? ReturnNotes { get; set; }

    public Instant CreatedAt { get; set; }
    public Instant UpdatedAt { get; set; }

    // One StockBinding per physical device unit allocated to this line item
    public List<StockBinding> StockBindings { get; set; } = new();
    public List<ChecklistItem> ChecklistItems { get; set; } = new();
    public List<RentalItemDamageReport> DamageReports { get; set; } = new();
}
