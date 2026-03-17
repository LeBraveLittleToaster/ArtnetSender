using NodaTime;

namespace LumenForgeServer.Rentals.Domain;

/// <summary>
/// Tracks which staff members assigned, picked up, dropped off, or completed a rental.
/// </summary>
public class RentalAssignment
{
    public string? AssignedByUserId { get; set; }
    public Instant? AssignedAt { get; set; }
    public string? PickupProcessedByUserId { get; set; }
    public string? DropoffProcessedByUserId { get; set; }
    public string? CompletedByUserId { get; set; }
}
