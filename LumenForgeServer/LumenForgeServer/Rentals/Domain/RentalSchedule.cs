using NodaTime;

namespace LumenForgeServer.Rentals.Domain;

/// <summary>
/// Planned and actual pickup/return timings for a rental.
/// </summary>
public class RentalSchedule
{
    public Instant? RequestedAt { get; set; }
    public Instant? PlannedPickupAt { get; set; }
    public Instant? PlannedReturnAt { get; set; }
    public Instant? PickupAt { get; set; }
    public Instant? DropoffAt { get; set; }
}
