using LumenForgeServer.Common;
using NodaTime;

namespace LumenForgeServer.Rentals.Domain;

/// <summary>
/// Immutable audit log entry recording a significant state change or action on a rental.
/// </summary>
public class RentalEvent
{
    public long Id { get; set; }
    public Guid Uuid { get; set; }

    public long RentalId { get; set; }
    public Rental Rental { get; set; } = null!;

    // Optional: the specific item this event relates to
    public long? RentalItemId { get; set; }
    public RentalItem? RentalItem { get; set; }

    public RentalEventType EventType { get; set; }
    public string? Description { get; set; }

    // Keycloak user id of the actor (null for system-generated events)
    public string? PerformedByUserId { get; set; }
    public Instant OccurredAt { get; set; }

    public Instant CreatedAt { get; set; }

    // Navigation to answers provided in response to this event
    public List<Answer> Answers { get; set; } = [];
}
