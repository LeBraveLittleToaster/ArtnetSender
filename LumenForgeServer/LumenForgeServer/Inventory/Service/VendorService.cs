using LumenForgeServer.Common.Exceptions;
using LumenForgeServer.Inventory.Dto.Create;
using LumenForgeServer.Inventory.Dto.Update;
using LumenForgeServer.Inventory.Dto.View;
using LumenForgeServer.Inventory.Factory;
using LumenForgeServer.Inventory.Persistance;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace LumenForgeServer.Inventory.Service;

/// <summary>
/// Application service for vendor operations.
/// </summary>
public class VendorService(IInventoryRepository repository)
{
    public async Task<VendorView> CreateVendor(CreateVendorDto dto, CancellationToken ct)
    {
        var vendor = VendorFactory.Create(dto);

        try
        {
            await repository.AddVendorAsync(vendor, ct);
            await repository.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            throw new UniqueConstraintException(ex.Message, ex);
        }

        return VendorView.FromEntity(vendor);
    }

    public async Task<VendorView> GetVendor(Guid vendorGuid, CancellationToken ct)
    {
        var vendor = await repository.GetVendorByGuidAsync(vendorGuid, ct)
            ?? throw new NotFoundException("Vendor not found.");

        return VendorView.FromEntity(vendor);
    }

    public async Task<(IReadOnlyList<VendorView> vendors, long total)> ListVendors(string? search, int limit, int offset, CancellationToken ct)
    {
        var vendors = await repository.ListVendorsAsync(search, limit, offset, ct);
        return (vendors.vendors.Select(VendorView.FromEntity).ToList(), vendors.total);
    }

    public async Task<VendorView> UpdateVendor(Guid vendorGuid, UpdateVendorDto dto, CancellationToken ct)
    {
        var vendor = await repository.GetVendorByGuidAsync(vendorGuid, ct)
            ?? throw new NotFoundException("Vendor not found.");

        vendor.Name = dto.Name;
        vendor.UpdatedAt = SystemClock.Instance.GetCurrentInstant();

        try
        {
            await repository.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            throw new UniqueConstraintException(ex.Message, ex);
        }

        return VendorView.FromEntity(vendor);
    }

    public async Task DeleteVendor(Guid vendorGuid, CancellationToken ct)
    {
        var vendor = await repository.GetVendorByGuidAsync(vendorGuid, ct)
            ?? throw new NotFoundException("Vendor not found.");

        await repository.DeleteVendorAsync(vendor, ct);
        await repository.SaveChangesAsync(ct);
    }
}
