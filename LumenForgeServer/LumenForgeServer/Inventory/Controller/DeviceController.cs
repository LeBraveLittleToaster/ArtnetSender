using LumenForgeServer.Auth.Domain;
using LumenForgeServer.Inventory.Dto.Create;
using LumenForgeServer.Inventory.Dto.Query;
using LumenForgeServer.Inventory.Dto.Update;
using LumenForgeServer.Inventory.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace LumenForgeServer.Inventory.Controller;

/// <summary>
/// HTTP API for managing inventory devices.
/// </summary>
/// <remarks>
/// Routes are under <c>api/v1/inventory/devices</c>.
/// </remarks>
[Route("api/v1/inventory/devices")]
[ApiController]
[Tags("Inventory – Devices")]
public class DeviceController(DeviceService deviceService) : ControllerBase
{
    /// <summary>
    /// Lists devices with optional paging and search.
    /// </summary>
    /// <remarks>
    /// Example query: <c>GET /api/v1/inventory/devices?search=serial-42&amp;limit=25&amp;offset=0</c>
    /// </remarks>
    /// <param name="query">Paging and search parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 200 response with device results.</returns>
    [HttpGet("")]
    [Authorize(Roles = nameof(Permissions.DeviceRead))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public async Task<IActionResult> ListDevices([FromQuery] ListQueryDto query, CancellationToken ct)
    {
        var (devices, total) = await deviceService.ListDevices(query.Search, query.Limit, query.Offset, ct);
        return Ok(new { list = devices, total });
    }

    /// <summary>
    /// Retrieves a single device by its GUID, including vendor, categories, stock bindings, and parameters.
    /// </summary>
    /// <param name="deviceGuid">Unique device identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 200 response with the device payload.</returns>
    [HttpGet("{deviceGuid:Guid}")]
    [Authorize(Roles = nameof(Permissions.DeviceRead))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> GetDevice([FromRoute] Guid deviceGuid, CancellationToken ct)
    {
        var device = await deviceService.GetDevice(deviceGuid, ct);
        return Ok(device);
    }

    /// <summary>
    /// Creates a new device together with its stock binding, parameters, and category associations.
    /// </summary>
    /// <remarks>
    /// The referenced vendor and categories must already exist.
    /// Side-effect: a stock binding row is created automatically.
    /// </remarks>
    /// <param name="dto">Device creation payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 201 response with the created device.</returns>
    [HttpPut("")]
    [Authorize(Roles = nameof(Permissions.DeviceCreate))]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public async Task<IActionResult> CreateDevice([FromBody] CreateDeviceDto dto, CancellationToken ct)
    {
        var device = await deviceService.CreateDevice(dto, ct);
        return CreatedAtAction(nameof(GetDevice), new { deviceGuid = device.Guid }, device);
    }

    /// <summary>
    /// Partially updates an existing device. At least one field must be provided.
    /// </summary>
    /// <param name="deviceGuid">Device to update.</param>
    /// <param name="dto">Fields to change.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 200 response with the updated device.</returns>
    [HttpPatch("{deviceGuid:Guid}")]
    [Authorize(Roles = nameof(Permissions.DeviceUpdate))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public async Task<IActionResult> UpdateDevice([FromRoute] Guid deviceGuid, [FromBody] UpdateDeviceDto dto, CancellationToken ct)
    {
        var device = await deviceService.UpdateDevice(deviceGuid, dto, ct);
        return Ok(device);
    }

    /// <summary>
    /// Permanently deletes a device and its associated stock bindings and parameters.
    /// </summary>
    /// <param name="deviceGuid">Device to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 204 response when deleted successfully.</returns>
    [HttpDelete("{deviceGuid:Guid}")]
    [Authorize(Roles = nameof(Permissions.DeviceDelete))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDevice([FromRoute] Guid deviceGuid, CancellationToken ct)
    {
        await deviceService.DeleteDevice(deviceGuid, ct);
        return NoContent();
    }

    /// <summary>
    /// Replaces all category assignments on a device. Existing links are removed first.
    /// </summary>
    /// <param name="deviceGuid">Device to update.</param>
    /// <param name="dto">Full list of category GUIDs to assign.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 200 response with the updated device.</returns>
    [HttpPut("{deviceGuid:Guid}/categories")]
    [Authorize(Roles = nameof(Permissions.DeviceUpdate))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> SetDeviceCategories([FromRoute] Guid deviceGuid, [FromBody] SetDeviceCategoriesDto dto, CancellationToken ct)
    {
        var device = await deviceService.SetDeviceCategories(deviceGuid, dto, ct);
        return Ok(device);
    }

    /// <summary>
    /// Creates or updates a key/value parameter on a device.
    /// If a parameter with the same key already exists it is overwritten.
    /// </summary>
    /// <param name="deviceGuid">Device that owns the parameter.</param>
    /// <param name="dto">Key and value to upsert.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 200 response with the upserted parameter.</returns>
    [HttpPut("{deviceGuid:Guid}/parameters")]
    [Authorize(Roles = nameof(Permissions.DeviceUpdate))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> UpsertDeviceParameter([FromRoute] Guid deviceGuid, [FromBody] UpsertDeviceParameterDto dto, CancellationToken ct)
    {
        var parameter = await deviceService.UpsertDeviceParameter(deviceGuid, dto, ct);
        return Ok(parameter);
    }

    /// <summary>
    /// Removes a parameter from a device by its key name.
    /// </summary>
    /// <param name="deviceGuid">Device that owns the parameter.</param>
    /// <param name="parameterKey">Key of the parameter to remove.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 204 response when removed successfully.</returns>
    [HttpDelete("{deviceGuid:Guid}/parameters/{parameterKey}")]
    [Authorize(Roles = nameof(Permissions.DeviceUpdate))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveDeviceParameter(
        [FromRoute] Guid deviceGuid,
        [FromRoute, Required, MinLength(1), RegularExpression(@".*\S.*")]
        string parameterKey,
        CancellationToken ct)
    {
        await deviceService.RemoveDeviceParameter(deviceGuid, parameterKey, ct);
        return NoContent();
    }   
}
