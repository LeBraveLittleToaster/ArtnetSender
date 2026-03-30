using LumenForgeServer.Common.Database;
using LumenForgeServer.Inventory.Domain;
using Microsoft.EntityFrameworkCore;

namespace LumenForgeServer.Inventory.Persistance;

/// <summary>
/// EF Core-backed repository for inventory entities.
/// </summary>
public sealed class InventoryRepository(AppDbContext db) : IInventoryRepository
{
    /// <summary>
    /// Executes the add category async operation.
    /// </summary>
    /// <remarks>Potential side effects: may modify state as part of this operation.</remarks>
    /// <param name="category">Input value used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task AddCategoryAsync(Category category, CancellationToken ct)
        => db.Categories.AddAsync(category, ct).AsTask();

    /// <summary>
    /// Executes the get category by guid async operation.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="categoryGuid">Unique identifier used to target the requested entity.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the Category? result.</returns>
    public Task<Category?> GetCategoryByGuidAsync(Guid categoryGuid, CancellationToken ct)
        => db.Categories.SingleOrDefaultAsync(c => c.Guid == categoryGuid, ct);

    /// <summary>
    /// Executes the task operation.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="search">Text input used by this operation.</param>
    /// <param name="limit">Numeric input used by this operation.</param>
    /// <param name="offset">Numeric input used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>The operation result.</returns>
    public async Task<(IReadOnlyList<Category> categories, long total)> ListCategoriesAsync(string? search, int limit, int offset, CancellationToken ct)
    {
        var query = db.Categories.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c =>
                c.Name.Contains(search) ||
                (c.Description != null && c.Description.Contains(search)));
        }

        var total = await query.LongCountAsync(ct);

        var categories = await query
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(ct);

        return (categories, total);
    }

    /// <summary>
    /// Executes the delete category async operation.
    /// </summary>
    /// <remarks>Potential side effects: may modify state as part of this operation.</remarks>
    /// <param name="category">Input value used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task DeleteCategoryAsync(Category category, CancellationToken ct)
    {
        db.Categories.Remove(category);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Executes the add vendor async operation.
    /// </summary>
    /// <remarks>Potential side effects: may modify state as part of this operation.</remarks>
    /// <param name="vendor">Input value used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task AddVendorAsync(Vendor vendor, CancellationToken ct)
        => db.Vendors.AddAsync(vendor, ct).AsTask();

    /// <summary>
    /// Executes the get vendor by guid async operation.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="vendorGuid">Unique identifier used to target the requested entity.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the Vendor? result.</returns>
    public Task<Vendor?> GetVendorByGuidAsync(Guid vendorGuid, CancellationToken ct)
        => db.Vendors.SingleOrDefaultAsync(v => v.Guid == vendorGuid, ct);

    /// <summary>
    /// Executes the task operation.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="search">Text input used by this operation.</param>
    /// <param name="limit">Numeric input used by this operation.</param>
    /// <param name="offset">Numeric input used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>The operation result.</returns>
    public async Task<(IReadOnlyList<Vendor> vendors, long total)> ListVendorsAsync(string? search, int limit, int offset, CancellationToken ct)
    {
        var query = db.Vendors.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(v => v.Name.Contains(search));
        }

        var total = await query.LongCountAsync(ct);

        var vendors = await query
            .AsNoTracking()
            .OrderBy(v => v.Name)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(ct);

        return (vendors, total);
    }

    /// <summary>
    /// Executes the delete vendor async operation.
    /// </summary>
    /// <remarks>Potential side effects: may modify state as part of this operation.</remarks>
    /// <param name="vendor">Input value used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task DeleteVendorAsync(Vendor vendor, CancellationToken ct)
    {
        db.Vendors.Remove(vendor);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Executes the try get vendor id by guid async operation.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="vendorGuid">Unique identifier used to target the requested entity.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the long? result.</returns>
    public Task<long?> TryGetVendorIdByGuidAsync(Guid vendorGuid, CancellationToken ct)
    {
        return db.Vendors
            .Where(v => v.Guid == vendorGuid)
            .Select(v => (long?)v.Id)
            .SingleOrDefaultAsync(ct);
    }

    /// <summary>
    /// Executes the try get maintenance status id by guid async operation.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="maintenanceStatusGuid">Unique identifier used to target the requested entity.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the long? result.</returns>
    public Task<long?> TryGetMaintenanceStatusIdByGuidAsync(Guid maintenanceStatusGuid, CancellationToken ct)
    {
        return db.MaintenanceStatuses
            .Where(ms => ms.Uuid == maintenanceStatusGuid)
            .Select(ms => (long?)ms.Id)
            .SingleOrDefaultAsync(ct);
    }

    /// <summary>
    /// Executes the try get any maintenance status id async operation.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the long? result.</returns>
    public Task<long?> TryGetAnyMaintenanceStatusIdAsync(CancellationToken ct)
    {
        return db.MaintenanceStatuses
            .OrderBy(ms => ms.Name)
            .Select(ms => (long?)ms.Id)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>
    /// Executes the add maintenance status async operation.
    /// </summary>
    /// <remarks>Potential side effects: may modify state as part of this operation.</remarks>
    /// <param name="maintenanceStatus">Numeric input used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task AddMaintenanceStatusAsync(MaintenanceStatus maintenanceStatus, CancellationToken ct)
        => db.MaintenanceStatuses.AddAsync(maintenanceStatus, ct).AsTask();

    /// <summary>
    /// Executes the get category ids by guids async operation.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="categoryGuids">Unique identifier used to target the requested entity.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the IReadOnlyList&lt;long&gt; result.</returns>
    public async Task<IReadOnlyList<long>> GetCategoryIdsByGuidsAsync(IReadOnlyCollection<Guid> categoryGuids, CancellationToken ct)
    {
        if (categoryGuids.Count == 0)
        {
            return Array.Empty<long>();
        }

        return await db.Categories
            .Where(c => categoryGuids.Contains(c.Guid))
            .Select(c => c.Id)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Executes the add device async operation.
    /// </summary>
    /// <remarks>Potential side effects: may modify state as part of this operation.</remarks>
    /// <param name="device">Input value used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task AddDeviceAsync(Device device, CancellationToken ct)
        => db.Devices.AddAsync(device, ct).AsTask();

    /// <summary>
    /// Executes the get device by guid async operation.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="deviceGuid">Unique identifier used to target the requested entity.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the Device? result.</returns>
    public Task<Device?> GetDeviceByGuidAsync(Guid deviceGuid, CancellationToken ct)
    {
        return BuildDeviceGraphQuery()
            .SingleOrDefaultAsync(d => d.Guid == deviceGuid, ct);
    }

    /// <summary>
    /// Executes the task operation.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="search">Text input used by this operation.</param>
    /// <param name="limit">Numeric input used by this operation.</param>
    /// <param name="offset">Numeric input used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>The operation result.</returns>
    public async Task<(IReadOnlyList<Device> devices, long total)> ListDevicesAsync(string? search, int limit, int offset, CancellationToken ct)
    {
        var query = BuildDeviceGraphQuery()
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(d =>
                d.SerialNumber.Contains(search) ||
                (d.DeviceName != null && d.DeviceName.Contains(search)) ||
                (d.DeviceDescription != null && d.DeviceDescription.Contains(search)) ||
                d.Vendor.Name.Contains(search) ||
                d.DeviceCategories.Any(dc => dc.Category.Name.Contains(search)));
        }

        var total = await query.LongCountAsync(ct);
        var devices = await query
            .OrderBy(d => d.SerialNumber)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(ct);

        return (devices, total);
    }

    /// <summary>
    /// Executes the delete device async operation.
    /// </summary>
    /// <remarks>Potential side effects: may modify state as part of this operation.</remarks>
    /// <param name="device">Input value used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task DeleteDeviceAsync(Device device, CancellationToken ct)
    {
        db.Devices.Remove(device);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Executes the try get device id by guid async operation.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="deviceGuid">Unique identifier used to target the requested entity.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the long? result.</returns>
    public Task<long?> TryGetDeviceIdByGuidAsync(Guid deviceGuid, CancellationToken ct)
        => db.Devices
            .Where(d => d.Guid == deviceGuid)
            .Select(d => (long?)d.Id)
            .SingleOrDefaultAsync(ct);

    /// <summary>
    /// Executes the replace device categories async operation.
    /// </summary>
    /// <remarks>Potential side effects: may modify state as part of this operation.</remarks>
    /// <param name="deviceId">Numeric input used by this operation.</param>
    /// <param name="categoryIds">Numeric input used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ReplaceDeviceCategoriesAsync(long deviceId, IReadOnlyCollection<long> categoryIds, CancellationToken ct)
    {
        var existing = await db.DeviceCategories
            .Where(dc => dc.DeviceId == deviceId)
            .ToListAsync(ct);

        db.DeviceCategories.RemoveRange(existing);

        var uniqueCategoryIds = categoryIds
            .Distinct()
            .ToArray();

        foreach (var categoryId in uniqueCategoryIds)
        {
            await db.DeviceCategories.AddAsync(new DeviceCategory
            {
                DeviceId = deviceId,
                CategoryId = categoryId
            }, ct);
        }
    }

    /// <summary>
    /// Executes the add device relation async operation.
    /// </summary>
    /// <remarks>Potential side effects: may modify state as part of this operation.</remarks>
    /// <param name="relation">Input value used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task AddDeviceRelationAsync(DeviceRelation relation, CancellationToken ct)
        => db.DeviceRelations.AddAsync(relation, ct).AsTask();

    /// <summary>
    /// Executes the add device relation audit log async operation.
    /// </summary>
    /// <remarks>Potential side effects: may modify state as part of this operation.</remarks>
    /// <param name="auditLog">Input value used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task AddDeviceRelationAuditLogAsync(DeviceRelationAuditLog auditLog, CancellationToken ct)
        => db.DeviceRelationAuditLogs.AddAsync(auditLog, ct).AsTask();

    /// <summary>
    /// Executes the get device relation by guid async operation.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="relationGuid">Unique identifier used to target the requested entity.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the DeviceRelation? result.</returns>
    public Task<DeviceRelation?> GetDeviceRelationByGuidAsync(Guid relationGuid, CancellationToken ct)
        => db.DeviceRelations
            .Include(r => r.ParentDevice)
            .Include(r => r.ChildDevice)
            .SingleOrDefaultAsync(r => r.Guid == relationGuid, ct);

    /// <summary>
    /// Executes the get active device relation async operation.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="parentDeviceId">Numeric input used by this operation.</param>
    /// <param name="childDeviceId">Numeric input used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the DeviceRelation? result.</returns>
    public Task<DeviceRelation?> GetActiveDeviceRelationAsync(long parentDeviceId, long childDeviceId, CancellationToken ct)
        => db.DeviceRelations.SingleOrDefaultAsync(r =>
            r.ParentDeviceId == parentDeviceId &&
            r.ChildDeviceId == childDeviceId, ct);

    /// <summary>
    /// Executes the get device relations by parent device id async operation.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="parentDeviceId">Numeric input used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the IReadOnlyList&lt;DeviceRelation&gt; result.</returns>
    public Task<IReadOnlyList<DeviceRelation>> GetDeviceRelationsByParentDeviceIdAsync(long parentDeviceId, CancellationToken ct)
    {
        return db.DeviceRelations
            .Include(r => r.ParentDevice)
            .Include(r => r.ChildDevice)
            .Where(r => r.ParentDeviceId == parentDeviceId)
            .OrderBy(r => r.ChildDevice.DeviceName)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<DeviceRelation>)t.Result, ct);
    }

    /// <summary>
    /// Executes the get active child device ids async operation.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="parentDeviceId">Numeric input used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the IReadOnlyList&lt;long&gt; result.</returns>
    public Task<IReadOnlyList<long>> GetActiveChildDeviceIdsAsync(long parentDeviceId, CancellationToken ct)
    {
        return db.DeviceRelations
            .Where(r => r.ParentDeviceId == parentDeviceId)
            .Select(r => r.ChildDeviceId)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<long>)t.Result, ct);
    }

    /// <summary>
    /// Executes the delete device relation async operation.
    /// </summary>
    /// <remarks>Potential side effects: may modify state as part of this operation.</remarks>
    /// <param name="relation">Input value used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task DeleteDeviceRelationAsync(DeviceRelation relation, CancellationToken ct)
    {
        db.DeviceRelations.Remove(relation);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Executes the save changes async operation.
    /// </summary>
    /// <remarks>Potential side effects: may modify state as part of this operation.</remarks>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SaveChangesAsync(CancellationToken ct)
        => db.SaveChangesAsync(ct);

    /// <summary>
    /// Executes the add stock binding async operation.
    /// </summary>
    /// <remarks>Potential side effects: may modify state as part of this operation.</remarks>
    /// <param name="stockBinding">Input value used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task AddStockBindingAsync(StockBinding stockBinding, CancellationToken ct)
        => db.StockBindings.AddAsync(stockBinding, ct).AsTask();

    /// <summary>
    /// Executes the add stock bindings async operation.
    /// </summary>
    /// <remarks>Potential side effects: may modify state as part of this operation.</remarks>
    /// <param name="stockBindings">Input value used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task AddStockBindingsAsync(IReadOnlyCollection<StockBinding> stockBindings, CancellationToken ct)
        => db.StockBindings.AddRangeAsync(stockBindings, ct);

    /// <summary>
    /// Executes the get stock binding by guid async operation.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="bindingGuid">Unique identifier used to target the requested entity.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the StockBinding? result.</returns>
    public Task<StockBinding?> GetStockBindingByGuidAsync(Guid bindingGuid, CancellationToken ct)
        => db.StockBindings.Include(sb => sb.Device).SingleOrDefaultAsync(sb => sb.Guid == bindingGuid, ct);

    /// <summary>
    /// Executes the get stock bindings by device guid async operation.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="deviceGuid">Unique identifier used to target the requested entity.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the IReadOnlyList&lt;StockBinding&gt; result.</returns>
    public Task<IReadOnlyList<StockBinding>> GetStockBindingsByDeviceGuidAsync(Guid deviceGuid, CancellationToken ct)
    {
        return db.StockBindings
            .Include(sb => sb.Device)
            .Where(sb => sb.Device.Guid == deviceGuid)
            .OrderBy(sb => sb.Start)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<StockBinding>)t.Result, ct);
    }

    /// <summary>
    /// Executes the get stock bindings by device id async operation.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="deviceId">Numeric input used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the IReadOnlyList&lt;StockBinding&gt; result.</returns>
    public Task<IReadOnlyList<StockBinding>> GetStockBindingsByDeviceIdAsync(long deviceId, CancellationToken ct)
    {
        return db.StockBindings
            .Where(sb => sb.DeviceId == deviceId)
            .OrderBy(sb => sb.Start)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<StockBinding>)t.Result, ct);
    }

    /// <summary>
    /// Executes the get stock bindings by owner process guid async operation.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="ownerProcessGuid">Unique identifier used to target the requested entity.</param>
    /// <param name="bindingType">Input value used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the IReadOnlyList&lt;StockBinding&gt; result.</returns>
    public Task<IReadOnlyList<StockBinding>> GetStockBindingsByOwnerProcessGuidAsync(Guid ownerProcessGuid, BindingType bindingType, CancellationToken ct)
    {
        return db.StockBindings
            .Include(sb => sb.Device)
            .Where(sb => sb.OwnerProcessGuid == ownerProcessGuid && sb.BindingType == bindingType)
            .OrderBy(sb => sb.Start)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<StockBinding>)t.Result, ct);
    }

    /// <summary>
    /// Executes the get overlapping reserved amount async operation.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="deviceId">Numeric input used by this operation.</param>
    /// <param name="start">Input value used by this operation.</param>
    /// <param name="end">Input value used by this operation.</param>
    /// <param name="bindingType">Input value used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the long result.</returns>
    public async Task<long> GetOverlappingReservedAmountAsync(long deviceId, NodaTime.Instant start, NodaTime.Instant end, BindingType bindingType, CancellationToken ct)
    {
        return await db.StockBindings
            .Where(sb => sb.DeviceId == deviceId &&
                         sb.BindingType == bindingType &&
                         sb.Start < end &&
                         sb.End > start)
            .SumAsync(sb => (long?)sb.ReservedAmount, ct) ?? 0L;
    }

    /// <summary>
    /// Executes the delete stock binding async operation.
    /// </summary>
    /// <remarks>Potential side effects: may modify state as part of this operation.</remarks>
    /// <param name="stockBinding">Input value used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task DeleteStockBindingAsync(StockBinding stockBinding, CancellationToken ct)
    {
        db.StockBindings.Remove(stockBinding);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Executes the has conflicting bindings async operation.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="deviceId">Numeric input used by this operation.</param>
    /// <param name="start">Input value used by this operation.</param>
    /// <param name="end">Input value used by this operation.</param>
    /// <param name="bindingType">Input value used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the bool result.</returns>
    public async Task<bool> HasConflictingBindingsAsync(long deviceId, NodaTime.Instant start, NodaTime.Instant end, BindingType bindingType, CancellationToken ct)
    {
        return await db.StockBindings.AnyAsync(sb =>
            sb.DeviceId == deviceId &&
            sb.BindingType == bindingType &&
            sb.Start < end &&
            sb.End > start, ct);
    }

    /// <summary>
    /// Executes the build device graph query operation.
    /// </summary>
    /// <remarks>Potential side effects: may modify state as part of this operation.</remarks>
    /// <returns>The operation result.</returns>
    private IQueryable<Device> BuildDeviceGraphQuery()
    {
        return db.Devices
            .Include(d => d.Vendor)
            .Include(d => d.MaintenanceStatus)
            .Include(d => d.StockBindings)
            .Include(d => d.Parameters)
            .Include(d => d.DeviceCategories)
            .ThenInclude(dc => dc.Category)
            .Include(d => d.ChildDeviceRelations)
            .ThenInclude(r => r.ChildDevice)
            .Include(d => d.ParentDeviceRelations)
            .ThenInclude(r => r.ParentDevice)
            .AsSplitQuery();
    }
}
