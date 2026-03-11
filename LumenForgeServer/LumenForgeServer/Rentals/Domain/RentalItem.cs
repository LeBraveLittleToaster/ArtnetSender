using LumenForgeServer.Maintenance.Domain;
using NodaTime;

namespace LumenForgeServer.Rentals.Domain;

/// <summary>
/// Represents a requested stock quantity line item within a rental.
/// </summary>
public class RentalItem
{
    public long Id { get; set; }
    public Guid Uuid { get; set; }

    public long RentalId { get; set; }
    public Rental Rental { get; set; } = null!;

    public bool IsApproved { get; set; }
    public Instant? ApprovedAt { get; set; }

    // nullable (customer or provider) - Keycloak user id
    public string? ApprovedByUserId { get; set; }

    public string? ConditionNotes { get; set; }

    public Instant CreatedAt { get; set; }
    public Instant UpdatedAt { get; set; }

    public List<ChecklistItem> ChecklistItems { get; set; } = new();
}
