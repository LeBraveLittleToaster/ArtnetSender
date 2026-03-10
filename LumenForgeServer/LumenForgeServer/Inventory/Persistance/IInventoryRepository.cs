using LumenForgeServer.Inventory.Domain;

namespace LumenForgeServer.Inventory.Persistance;

/// <summary>
/// Persistence contract for inventory entities.
/// </summary>
public interface IInventoryRepository
{
    Task AddCategoryAsync(Category category, CancellationToken ct);
    Task<Category?> GetCategoryByGuidAsync(Guid categoryGuid, CancellationToken ct);
    Task<IReadOnlyList<Category>> ListCategoriesAsync(string? search, int limit, int offset, CancellationToken ct);
    Task DeleteCategoryAsync(Category category, CancellationToken ct);

    Task AddVendorAsync(Vendor vendor, CancellationToken ct);
    Task<Vendor?> GetVendorByGuidAsync(Guid vendorGuid, CancellationToken ct);
    Task<IReadOnlyList<Vendor>> ListVendorsAsync(string? search, int limit, int offset, CancellationToken ct);
    Task DeleteVendorAsync(Vendor vendor, CancellationToken ct);
    Task<long?> TryGetVendorIdByGuidAsync(Guid vendorGuid, CancellationToken ct);

    Task<long?> TryGetMaintenanceStatusIdByGuidAsync(Guid maintenanceStatusGuid, CancellationToken ct);
    Task<long?> TryGetAnyMaintenanceStatusIdAsync(CancellationToken ct);
    Task AddMaintenanceStatusAsync(MaintenanceStatus maintenanceStatus, CancellationToken ct);

    Task<IReadOnlyList<long>> GetCategoryIdsByGuidsAsync(IReadOnlyCollection<Guid> categoryGuids, CancellationToken ct);

    Task AddDeviceAsync(Device device, CancellationToken ct);
    Task<Device?> GetDeviceByGuidAsync(Guid deviceGuid, CancellationToken ct);
    Task<IReadOnlyList<Device>> ListDevicesAsync(string? search, int limit, int offset, CancellationToken ct);
    Task DeleteDeviceAsync(Device device, CancellationToken ct);
    Task ReplaceDeviceCategoriesAsync(long deviceId, IReadOnlyCollection<long> categoryIds, CancellationToken ct);
    Task<long?> TryGetDeviceIdByGuidAsync(Guid deviceGuid, CancellationToken ct);

    Task AddStockBindingAsync(StockBinding stockBinding, CancellationToken ct);
    Task AddStockBindingsAsync(IReadOnlyCollection<StockBinding> stockBindings, CancellationToken ct);
    Task<StockBinding?> GetStockBindingByGuidAsync(Guid bindingGuid, CancellationToken ct);
    Task<IReadOnlyList<StockBinding>> GetStockBindingsByDeviceGuidAsync(Guid deviceGuid, CancellationToken ct);
    Task<IReadOnlyList<StockBinding>> GetStockBindingsByDeviceIdAsync(long deviceId, CancellationToken ct);
    Task DeleteStockBindingAsync(StockBinding stockBinding, CancellationToken ct);
    Task<bool> HasConflictingBindingsAsync(long deviceId, NodaTime.Instant start, NodaTime.Instant end, BindingType bindingType, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
