using LumenForgeServer.Common;
using LumenForgeServer.Inventory.Domain;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Dto.Query;
using NodaTime;

namespace LumenForgeServer.Rentals.Persistence;

/// <summary>
/// Persistence contract for rental entities.
/// </summary>
public interface IRentalRepository
{
    Task AddRentalAsync(Rental rental, CancellationToken ct);
    Task<Rental?> GetRentalByGuidAsync(Guid rentalGuid, RentalInclude include, CancellationToken ct);
    Task<long?> TryGetRentalStatusIdByGuidAsync(Guid statusGuid, CancellationToken ct);

    Task<(IReadOnlyList<Rental> items, long total)> ListRentalsAsync(
        string? search,
        string? customerUserId,
        RentalPriority? priority,
        int limit,
        int offset,
        RentalInclude include,
        CancellationToken ct);

    Task DeleteRentalAsync(Rental rental, CancellationToken ct);

    // Checklists
    Task AddChecklistAsync(Checklist checklist, CancellationToken ct);
    Task<bool> RentalExistsByGuidAsync(Guid rentalGuid, CancellationToken ct);
    Task<Checklist?> GetChecklistByGuidAsync(Guid rentalGuid, Guid checklistGuid, CancellationToken ct);
    Task<IReadOnlyList<Checklist>> ListChecklistsForRentalAsync(Guid rentalGuid, CancellationToken ct);
    Task<ChecklistItem?> GetChecklistItemByGuidAsync(Guid rentalGuid, Guid checklistGuid, Guid itemGuid, CancellationToken ct);

    /// <summary>
    /// Finds the checklist item whose rental item has a stock binding for <paramref name="deviceGuid"/>
    /// on the given checklist. Used by the QR-scan lookup endpoint.
    /// </summary>
    Task<ChecklistItem?> GetChecklistItemByDeviceGuidAsync(Guid rentalGuid, Guid checklistGuid, Guid deviceGuid, CancellationToken ct);

    /// <summary>
    /// Returns all stock bindings that overlap the given window for the specified device and type.
    /// Used to surface booking conflicts before a rental item is approved.
    /// </summary>
    Task<(IReadOnlyList<StockBinding> items, long total)> ListConflictingBindingsAsync(
        long deviceId,
        Instant start,
        Instant end,
        BindingType bindingType,
        int limit,
        int offset,
        CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
