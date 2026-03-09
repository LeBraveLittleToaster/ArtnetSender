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
        var devices = await deviceService.ListDevices(query.Search, query.Limit, query.Offset, ct);
        return Ok(devices);
    }

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

    [HttpPut("")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Produces("application/json")]
    public async Task<IActionResult> CreateDevice([FromBody] CreateDeviceDto dto, CancellationToken ct)
    {
        var device = await deviceService.CreateDevice(dto, ct);
        return CreatedAtAction(nameof(GetDevice), new { deviceGuid = device.Guid }, device);
    }

    [HttpPatch("{deviceGuid:Guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Produces("application/json")]
    public async Task<IActionResult> UpdateDevice([FromRoute] Guid deviceGuid, [FromBody] UpdateDeviceDto dto, CancellationToken ct)
    {
        var device = await deviceService.UpdateDevice(deviceGuid, dto, ct);
        return Ok(device);
    }

    [HttpDelete("{deviceGuid:Guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> DeleteDevice([FromRoute] Guid deviceGuid, CancellationToken ct)
    {
        await deviceService.DeleteDevice(deviceGuid, ct);
        return NoContent();
    }

    [HttpPut("{deviceGuid:Guid}/categories")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> SetCategories([FromRoute] Guid deviceGuid, [FromBody] SetDeviceCategoriesDto dto, CancellationToken ct)
    {
        var device = await deviceService.SetDeviceCategories(deviceGuid, dto, ct);
        return Ok(device);
    }

    [HttpPut("{deviceGuid:Guid}/parameters")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Produces("application/json")]
    public async Task<IActionResult> UpsertParameter([FromRoute] Guid deviceGuid, [FromBody] UpsertDeviceParameterDto dto, CancellationToken ct)
    {
        var parameter = await deviceService.UpsertDeviceParameter(deviceGuid, dto, ct);
        return Ok(parameter);
    }

    [HttpDelete("{deviceGuid:Guid}/parameters/{parameterKey}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> RemoveParameter(
        [FromRoute] Guid deviceGuid,
        [FromRoute, Required, MinLength(1), RegularExpression(@".*\S.*")]
        string parameterKey,
        CancellationToken ct)
    {
        await deviceService.RemoveDeviceParameter(deviceGuid, parameterKey, ct);
        return NoContent();
    }

}
