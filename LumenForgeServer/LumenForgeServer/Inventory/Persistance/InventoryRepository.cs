using LumenForgeServer.Common.Database;
using LumenForgeServer.Inventory.Domain;
using Microsoft.EntityFrameworkCore;

namespace LumenForgeServer.Inventory.Persistance;

/// <summary>
/// EF Core-backed repository for inventory entities.
/// </summary>
public sealed class InventoryRepository(AppDbContext db) : IInventoryRepository
{
    public Task AddCategoryAsync(Category category, CancellationToken ct)
        => db.Categories.AddAsync(category, ct).AsTask();

    public Task<Category?> GetCategoryByGuidAsync(Guid categoryGuid, CancellationToken ct)
        => db.Categories.SingleOrDefaultAsync(c => c.Guid == categoryGuid, ct);

    public async Task<IReadOnlyList<Category>> ListCategoriesAsync(string? search, int limit, int offset, CancellationToken ct)
    {
        var query = db.Categories.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c =>
                c.Name.Contains(search) ||
                (c.Description != null && c.Description.Contains(search)));
        }

        return await query
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(ct);
    }

    public Task DeleteCategoryAsync(Category category, CancellationToken ct)
    {
        db.Categories.Remove(category);
        return Task.CompletedTask;
    }

    public Task AddVendorAsync(Vendor vendor, CancellationToken ct)
        => db.Vendors.AddAsync(vendor, ct).AsTask();

    public Task<Vendor?> GetVendorByGuidAsync(Guid vendorGuid, CancellationToken ct)
        => db.Vendors.SingleOrDefaultAsync(v => v.Guid == vendorGuid, ct);

    public async Task<IReadOnlyList<Vendor>> ListVendorsAsync(string? search, int limit, int offset, CancellationToken ct)
    {
        var query = db.Vendors.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(v => v.Name.Contains(search));
        }

        return await query
            .AsNoTracking()
            .OrderBy(v => v.Name)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(ct);
    }

    public Task DeleteVendorAsync(Vendor vendor, CancellationToken ct)
    {
        db.Vendors.Remove(vendor);
        return Task.CompletedTask;
    }

    public Task<long?> TryGetVendorIdByGuidAsync(Guid vendorGuid, CancellationToken ct)
    {
        return db.Vendors
            .Where(v => v.Guid == vendorGuid)
            .Select(v => (long?)v.Id)
            .SingleOrDefaultAsync(ct);
    }

    public Task<long?> TryGetMaintenanceStatusIdByGuidAsync(Guid maintenanceStatusGuid, CancellationToken ct)
    {
        return db.MaintenanceStatuses
            .Where(ms => ms.Uuid == maintenanceStatusGuid)
            .Select(ms => (long?)ms.Id)
            .SingleOrDefaultAsync(ct);
    }

    public Task<long?> TryGetAnyMaintenanceStatusIdAsync(CancellationToken ct)
    {
        return db.MaintenanceStatuses
            .OrderBy(ms => ms.Name)
            .Select(ms => (long?)ms.Id)
            .FirstOrDefaultAsync(ct);
    }

    public Task AddMaintenanceStatusAsync(MaintenanceStatus maintenanceStatus, CancellationToken ct)
        => db.MaintenanceStatuses.AddAsync(maintenanceStatus, ct).AsTask();

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

    public Task AddDeviceAsync(Device device, CancellationToken ct)
        => db.Devices.AddAsync(device, ct).AsTask();

    public Task<Device?> GetDeviceByGuidAsync(Guid deviceGuid, CancellationToken ct)
    {
        return BuildDeviceGraphQuery()
            .SingleOrDefaultAsync(d => d.Guid == deviceGuid, ct);
    }

    public async Task<IReadOnlyList<Device>> ListDevicesAsync(string? search, int limit, int offset, CancellationToken ct)
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

        return await query
            .OrderBy(d => d.SerialNumber)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(ct);
    }

    public Task DeleteDeviceAsync(Device device, CancellationToken ct)
    {
        db.Devices.Remove(device);
        return Task.CompletedTask;
    }

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

    public Task SaveChangesAsync(CancellationToken ct)
        => db.SaveChangesAsync(ct);

    public Task AddStockBindingAsync(StockBinding stockBinding, CancellationToken ct)
        => db.StockBindings.AddAsync(stockBinding, ct).AsTask();

    public Task AddStockBindingsAsync(IReadOnlyCollection<StockBinding> stockBindings, CancellationToken ct)
        => db.StockBindings.AddRangeAsync(stockBindings, ct);

    public Task<StockBinding?> GetStockBindingByGuidAsync(Guid bindingGuid, CancellationToken ct)
        => db.StockBindings.Include(sb => sb.Device).SingleOrDefaultAsync(sb => sb.Guid == bindingGuid, ct);

    public Task<IReadOnlyList<StockBinding>> GetStockBindingsByDeviceGuidAsync(Guid deviceGuid, CancellationToken ct)
    {
        return db.StockBindings
            .Include(sb => sb.Device)
            .Where(sb => sb.Device.Guid == deviceGuid)
            .OrderBy(sb => sb.Start)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<StockBinding>)t.Result, ct);
    }

    public Task<IReadOnlyList<StockBinding>> GetStockBindingsByDeviceIdAsync(long deviceId, CancellationToken ct)
    {
        return db.StockBindings
            .Where(sb => sb.DeviceId == deviceId)
            .OrderBy(sb => sb.Start)
            .ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<StockBinding>)t.Result, ct);
    }

    public Task DeleteStockBindingAsync(StockBinding stockBinding, CancellationToken ct)
    {
        db.StockBindings.Remove(stockBinding);
        return Task.CompletedTask;
    }

    public async Task<bool> HasConflictingBindingsAsync(long deviceId, NodaTime.Instant start, NodaTime.Instant end, BindingType bindingType, CancellationToken ct)
    {
        return await db.StockBindings.AnyAsync(sb =>
            sb.DeviceId == deviceId &&
            sb.BindingType == bindingType &&
            sb.Start < end &&
            sb.End > start, ct);
    }

    private IQueryable<Device> BuildDeviceGraphQuery()
    {
        return db.Devices
            .Include(d => d.Vendor)
            .Include(d => d.MaintenanceStatus)
            .Include(d => d.StockBindings)
            .Include(d => d.Parameters)
            .Include(d => d.DeviceCategories)
            .ThenInclude(dc => dc.Category)
            .AsSplitQuery();
    }
}
