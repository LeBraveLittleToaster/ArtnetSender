using LumenForgeServer.Auth.Domain;
using LumenForgeServer.Auth.Dto.Views;
using LumenForgeServer.Inventory.Dto.Create;
using LumenForgeServer.Inventory.Dto.Query;
using LumenForgeServer.Inventory.Dto.Update;
using LumenForgeServer.Inventory.Dto.View;
using LumenForgeServer.Inventory.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LumenForgeServer.Inventory.Controller;

/// <summary>
/// HTTP API for managing inventory vendors.
/// </summary>
/// <remarks>
/// Routes are under <c>api/v1/inventory/vendors</c>.
/// </remarks>
[Route("api/v1/inventory/vendors")]
[ApiController]
[Tags("Inventory – Vendors")]
public class VendorController(VendorService vendorService) : ControllerBase
{
    /// <summary>
    /// Lists vendors with optional paging and search.
    /// </summary>
    /// <remarks>
    /// Example query: <c>GET /api/v1/inventory/vendors?search=sony&amp;limit=25&amp;offset=0</c>
    /// </remarks>
    /// <param name="query">Paging and search parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 200 response with vendor results.</returns>
    [HttpGet("")]
    [Authorize(Roles = nameof(Permissions.VendorRead))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public async Task<IActionResult> ListVendors([FromQuery] ListQueryDto query, CancellationToken ct)
    {
        var vendors = await vendorService.ListVendors(query.Search, query.Limit, query.Offset, ct);
        return Ok(new ListViewDto<VendorView> { list = vendors.vendors, total = vendors.total });
    }

    /// <summary>
    /// Retrieves a single vendor by its GUID.
    /// </summary>
    /// <param name="vendorGuid">Unique vendor identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 200 response with the vendor payload.</returns>
    [HttpGet("{vendorGuid:Guid}")]
    [Authorize(Roles = nameof(Permissions.VendorRead))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> GetVendor([FromRoute] Guid vendorGuid, CancellationToken ct)
    {
        var vendor = await vendorService.GetVendor(vendorGuid, ct);
        return Ok(vendor);
    }

    /// <summary>
    /// Creates a new vendor.
    /// </summary>
    /// <param name="dto">Vendor creation payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 201 response with the created vendor.</returns>
    [HttpPut("")]
    [Authorize(Roles = nameof(Permissions.VendorCreate))]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public async Task<IActionResult> CreateVendor([FromBody] CreateVendorDto dto, CancellationToken ct)
    {
        var vendor = await vendorService.CreateVendor(dto, ct);
        return CreatedAtAction(nameof(GetVendor), new { vendorGuid = vendor.Guid }, vendor);
    }

    /// <summary>
    /// Updates an existing vendor's name.
    /// </summary>
    /// <param name="vendorGuid">Vendor to update.</param>
    /// <param name="dto">Fields to change.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 200 response with the updated vendor.</returns>
    [HttpPatch("{vendorGuid:Guid}")]
    [Authorize(Roles = nameof(Permissions.VendorUpdate))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public async Task<IActionResult> UpdateVendor([FromRoute] Guid vendorGuid, [FromBody] UpdateVendorDto dto, CancellationToken ct)
    {
        var vendor = await vendorService.UpdateVendor(vendorGuid, dto, ct);
        return Ok(vendor);
    }

    /// <summary>
    /// Permanently deletes a vendor. Devices referencing this vendor must be re-assigned first.
    /// </summary>
    /// <param name="vendorGuid">Vendor to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 204 response when deleted successfully.</returns>
    [HttpDelete("{vendorGuid:Guid}")]
    [Authorize(Roles = nameof(Permissions.VendorDelete))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteVendor([FromRoute] Guid vendorGuid, CancellationToken ct)
    {
        await vendorService.DeleteVendor(vendorGuid, ct);
        return NoContent();
    }
}
