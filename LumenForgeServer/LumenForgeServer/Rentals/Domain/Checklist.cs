using LumenForgeServer.Common;
using NodaTime;

namespace LumenForgeServer.Rentals.Domain;

/// <summary>
/// A pickup or dropoff checklist associated with a rental process.
/// Items are generated from the assigned stock bindings and must be
/// scanned/signed before the rental can proceed.
/// </summary>
public class Checklist
{
    /// <summary>Database primary key.</summary>
    public long Id { get; set; }

    /// <summary>Public identifier.</summary>
    public Guid Guid { get; set; }

    /// <summary>Foreign key to the owning process instance.</summary>
    public long ProcessInstanceId { get; set; }

    /// <summary>Navigation to the owning process instance.</summary>
    public RentalProcessInstance ProcessInstance { get; set; } = null!;

    /// <summary>Type of checklist (pickup or dropoff).</summary>
    public ChecklistType ChecklistType { get; set; }

    /// <summary>Whether the checklist has been signed.</summary>
    public bool IsSigned { get; set; }

    /// <summary>Keycloak id of the user who signed.</summary>
    public string? SignedByKcId { get; set; }

    /// <summary>Base64-encoded signature data or textual acknowledgement.</summary>
    public string? SignatureData { get; set; }

    /// <summary>Instant the checklist was signed.</summary>
    public Instant? SignedAt { get; set; }

    /// <summary>Instant the checklist was created.</summary>
    public Instant CreatedAt { get; set; }

    /// <summary>Line items for this checklist.</summary>
    public List<ChecklistItem> Items { get; set; } = [];
}
