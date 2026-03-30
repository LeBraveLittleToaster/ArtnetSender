using LumenForgeServer.Billing.Domain;
using LumenForgeServer.Common;
using NodaTime;

namespace LumenForgeServer.Rentals.Domain;

/// <summary>
/// Core data aggregate representing a rental. Holds all customer-facing information
/// such as the requested period, priority, and free-text notes.
/// Linked 1 : 1 with a <see cref="RentalProcessInstance"/> that drives the workflow.
/// </summary>
public class Rental
{
    /// <summary>Database primary key.</summary>
    public long Id { get; set; }

    /// <summary>Public identifier used in API responses.</summary>
    public Guid Uuid { get; set; }

    /// <summary>Keycloak subject id of the customer who owns the rental.</summary>
    public required string CustomerKcId { get; set; }

    /// <summary>Optional owning group GUID for group-owned rentals.</summary>
    public Guid? GroupGuid { get; set; }

    /// <summary>Display name of the customer (denormalized for convenience).</summary>
    public string? CustomerName { get; set; }

    /// <summary>Contact email for rental-related communication.</summary>
    public string? CustomerEmail { get; set; }

    /// <summary>Free-text description of the rental purpose.</summary>
    public string? Purpose { get; set; }

    /// <summary>Requested start of the rental period.</summary>
    public Instant RequestedStart { get; set; }

    /// <summary>Requested end of the rental period.</summary>
    public Instant RequestedEnd { get; set; }

    /// <summary>Priority level of the rental request.</summary>
    public RentalPriority Priority { get; set; }

    /// <summary>Optional free-text notes attached by the customer or staff.</summary>
    public string? Notes { get; set; }
    
    /// <summary>Instant the rental was created.</summary>
    public Instant CreatedAt { get; set; }

    /// <summary>Instant the rental was last modified.</summary>
    public Instant UpdatedAt { get; set; }

    // ── Navigation ──────────────────────────────────────────────────

    /// <summary>The process instance driving this rental's workflow.</summary>
    public RentalProcessInstance? ProcessInstance { get; set; }
    
    public List<Answer> Answers { get; set; } = [];

    /// <summary>Invoices generated for this rental.</summary>
    public List<Invoice> Invoices { get; set; } = [];
}
