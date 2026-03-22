using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Dto.Query;
using LumenForgeServer.Rentals.Service;
using LumenForgeServer.Rentals.Service.Actions;
using NodaTime;

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

    /// <summary>
    /// Loads a process instance with selectively included navigation properties
    /// based on the <paramref name="includes"/> flags.
    /// </summary>
    Task<RentalProcessInstance?> GetByGuidWithIncludesAsync(
        Guid processGuid, RentalProcessInclude includes, CancellationToken ct);

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

    // ── Query / read-only methods ────────────────────────────────────

    /// <summary>
    /// Lists process instances with optional paging, search, sorting, date-range,
    /// owner, and stage filtering. Includes the linked <see cref="Rental"/> for
    /// summary projection.
    /// </summary>
    Task<(List<RentalProcessInstance> Items, int Total)> ListAsync(
        RentalListQueryDto query, CancellationToken ct);

    /// <summary>Returns the count of processes grouped by <see cref="RentalStage"/>.</summary>
    Task<Dictionary<RentalStage, int>> CountByStageAsync(CancellationToken ct);

    /// <summary>Returns the total number of damage reports.</summary>
    Task<int> CountDamageReportsAsync(CancellationToken ct);

    /// <summary>Returns the total number of extension requests.</summary>
    Task<int> CountExtensionsAsync(CancellationToken ct);

    /// <summary>Returns the number of extensions that have not yet been reviewed.</summary>
    Task<int> CountPendingExtensionsAsync(CancellationToken ct);

    /// <summary>Returns the total number of action log entries.</summary>
    Task<int> CountActionLogsAsync(CancellationToken ct);

    /// <summary>Returns the number of processes created on or after <paramref name="since"/>.</summary>
    Task<int> CountProcessesCreatedSinceAsync(Instant since, CancellationToken ct);

    /// <summary>Returns the number of action log entries recorded on or after <paramref name="since"/>.</summary>
    Task<int> CountActionLogsSinceAsync(Instant since, CancellationToken ct);

    /// <summary>Returns the number of damage reports filed on or after <paramref name="since"/>.</summary>
    Task<int> CountDamageReportsSinceAsync(Instant since, CancellationToken ct);

    /// <summary>
    /// Returns the number of processes that reached a specific stage via an action
    /// recorded on or after <paramref name="since"/>.
    /// </summary>
    Task<int> CountProcessesReachedStageSinceAsync(RentalStage stage, Instant since, CancellationToken ct);

    /// <summary>Returns action log entries for a given process with optional paging, ordered by date descending.</summary>
    Task<(List<RentalActionLog> Items, int Total)> GetActionLogsByProcessGuidAsync(
        Guid processGuid, int limit, int offset, CancellationToken ct);
}
