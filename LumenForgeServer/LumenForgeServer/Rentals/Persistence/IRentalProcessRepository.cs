using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Service;

namespace LumenForgeServer.Rentals.Persistence;

/// <summary>
/// Data-access contract for <see cref="RentalProcessInstance"/> and its related
/// entities. Used by action handlers and the <see cref="RentalActionService"/> orchestrator.
/// </summary>
public interface IRentalProcessRepository
{
    /// <summary>
    /// Loads a process instance by its public GUID, including the linked
    /// <see cref="Rental"/> aggregate.
    /// </summary>
    Task<RentalProcessInstance?> GetByGuidAsync(Guid processGuid, CancellationToken ct);

    /// <summary>
    /// Loads a process instance with all navigation properties populated
    /// (Rental, Checklists + Items, Extensions, DamageReports).
    /// </summary>
    Task<RentalProcessInstance?> GetByGuidWithDetailsAsync(Guid processGuid, CancellationToken ct);

    /// <summary>Persists a newly created <see cref="RentalProcessInstance"/>.</summary>
    Task AddAsync(RentalProcessInstance instance, CancellationToken ct);

    /// <summary>Persists changes to an existing <see cref="RentalProcessInstance"/>.</summary>
    Task UpdateAsync(RentalProcessInstance instance, CancellationToken ct);

    /// <summary>Appends an audit log entry.</summary>
    Task AddActionLogAsync(RentalActionLog log, CancellationToken ct);

    /// <summary>Adds a rental data aggregate to the database.</summary>
    Task AddRentalAsync(Rental rental, CancellationToken ct);

    /// <summary>Adds a checklist to the database.</summary>
    Task AddChecklistAsync(Checklist checklist, CancellationToken ct);

    /// <summary>Loads a checklist by its public GUID, including items.</summary>
    Task<Checklist?> GetChecklistByGuidAsync(Guid checklistGuid, CancellationToken ct);

    /// <summary>Adds an extension request to the database.</summary>
    Task AddExtensionAsync(RentalExtension extension, CancellationToken ct);

    /// <summary>Loads an extension by its public GUID.</summary>
    Task<RentalExtension?> GetExtensionByGuidAsync(Guid extensionGuid, CancellationToken ct);

    /// <summary>Adds damage reports to the database.</summary>
    Task AddDamageReportsAsync(IEnumerable<RentalDamageReport> reports, CancellationToken ct);

    /// <summary>Saves all pending changes in the current unit of work.</summary>
    Task SaveChangesAsync(CancellationToken ct);
}
