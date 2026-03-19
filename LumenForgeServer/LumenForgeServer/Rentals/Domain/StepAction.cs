using NodaTime;

namespace LumenForgeServer.Rentals.Domain;

/// <summary>
/// Abstract base for every rental action recorded against a <see cref="Rental"/>.
/// Concrete subtypes carry action-specific companion data in their own TPT tables.
/// </summary>
public abstract class StepAction
{
    public long Id { get; set; }
    public Guid Uuid { get; set; }

    public ActionType ActionType { get; set; }

    public List<Companion> Companions { get; set; } = null!;

    /// <summary>Keycloak user id of the actor (null for system-generated actions).</summary>
    public string? PerformedByUserId { get; set; }

    public Instant ExecutedAt { get; set; }
    public Instant CreatedAt { get; set; }
}
