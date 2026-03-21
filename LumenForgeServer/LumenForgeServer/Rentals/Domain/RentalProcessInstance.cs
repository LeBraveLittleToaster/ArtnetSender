using NodaTime;

namespace LumenForgeServer.Rentals.Domain;

/// <summary>
/// Tracks a single rental process from creation to completion.
/// The <see cref="CurrentStage"/> determines which actions are available next,
/// forming an implicit process definition without a formal notation.
/// </summary>
/// <remarks>
/// Every action executed through the framework updates this instance and
/// appends a <see cref="RentalActionLog"/> entry for auditing.
/// </remarks>
public class RentalProcessInstance
{
    /// <summary>Database primary key.</summary>
    public long Id { get; set; }

    /// <summary>Public identifier used in API routes and cross-service references.</summary>
    public Guid Guid { get; set; }

    /// <summary>Current stage of the rental workflow.</summary>
    public RentalStage CurrentStage { get; set; } = RentalStage.None;

    /// <summary>Keycloak subject id of the user who initiated the process.</summary>
    public required string CreatedByKcId { get; set; }

    /// <summary>Instant the process was created.</summary>
    public Instant CreatedAt { get; set; }

    /// <summary>Instant the process was last modified by any action.</summary>
    public Instant UpdatedAt { get; set; }

    // ── Navigation ──────────────────────────────────────────────────

    /// <summary>Foreign key to the associated <see cref="Rental"/> data.</summary>
    public long? RentalId { get; set; }

    /// <summary>The rental data aggregate linked to this process.</summary>
    public Rental? Rental { get; set; }

    /// <summary>Chronological log of every action executed on this process.</summary>
    public List<RentalActionLog> ActionLogs { get; set; } = [];

    /// <summary>Checklists generated for this process.</summary>
    public List<Checklist> Checklists { get; set; } = [];

    /// <summary>Extension requests submitted for this process.</summary>
    public List<RentalExtension> Extensions { get; set; } = [];

    /// <summary>Damage reports recorded for this process.</summary>
    public List<RentalDamageReport> DamageReports { get; set; } = [];
}
