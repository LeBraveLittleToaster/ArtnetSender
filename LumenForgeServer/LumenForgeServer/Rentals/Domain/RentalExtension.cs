using NodaTime;

namespace LumenForgeServer.Rentals.Domain;

/// <summary>
/// Represents a customer request to extend the return date of an active rental.
/// </summary>
public class RentalExtension
{
    public long Id { get; set; }
    public Guid Uuid { get; set; }

    public long RentalId { get; set; }
    public Rental Rental { get; set; } = null!;

    public Instant OriginalReturnAt { get; set; }
    public Instant RequestedReturnAt { get; set; }
    public Instant? ApprovedReturnAt { get; set; }

    public string? Reason { get; set; }

    // Keycloak user id
    public string RequestedByUserId { get; set; } = null!;
    public Instant RequestedAt { get; set; }

    public bool IsApproved { get; set; }
    public string? ApprovedByUserId { get; set; }
    public Instant? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }

    public Instant CreatedAt { get; set; }
    public Instant UpdatedAt { get; set; }
}
