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
    /// <summary>
    /// Executes the create vendor operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: may persist state changes, emit workflow logs, or call external dependencies.</remarks>
    /// <param name="dto">Request payload containing the input data required for the operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the VendorView result.</returns>
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

    /// <summary>
    /// Executes the get vendor operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="vendorGuid">Unique identifier used to target the requested entity.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the VendorView result.</returns>
    public async Task<VendorView> GetVendor(Guid vendorGuid, CancellationToken ct)
    {
        var vendor = await repository.GetVendorByGuidAsync(vendorGuid, ct)
            ?? throw new NotFoundException("Vendor not found.");

        return VendorView.FromEntity(vendor);
    }

    /// <summary>
    /// Executes the task operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="search">Text input used by this operation.</param>
    /// <param name="limit">Numeric input used by this operation.</param>
    /// <param name="offset">Numeric input used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>The operation result.</returns>
    public async Task<(IReadOnlyList<VendorView> vendors, long total)> ListVendors(string? search, int limit, int offset, CancellationToken ct)
    {
        var vendors = await repository.ListVendorsAsync(search, limit, offset, ct);
        return (vendors.vendors.Select(VendorView.FromEntity).ToList(), vendors.total);
    }

    /// <summary>
    /// Executes the update vendor operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: may persist state changes, emit workflow logs, or call external dependencies.</remarks>
    /// <param name="vendorGuid">Unique identifier used to target the requested entity.</param>
    /// <param name="dto">Request payload containing the input data required for the operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the VendorView result.</returns>
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

    /// <summary>
    /// Executes the delete vendor operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: may persist state changes, emit workflow logs, or call external dependencies.</remarks>
    /// <param name="vendorGuid">Unique identifier used to target the requested entity.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task DeleteVendor(Guid vendorGuid, CancellationToken ct)
    {
        var vendor = await repository.GetVendorByGuidAsync(vendorGuid, ct)
            ?? throw new NotFoundException("Vendor not found.");

        await repository.DeleteVendorAsync(vendor, ct);
        await repository.SaveChangesAsync(ct);
    }
}
