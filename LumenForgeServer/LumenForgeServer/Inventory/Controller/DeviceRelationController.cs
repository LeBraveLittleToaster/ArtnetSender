using LumenForgeServer.Auth.Domain;
using LumenForgeServer.Inventory.Dto.Create;
using LumenForgeServer.Inventory.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LumenForgeServer.Inventory.Controller;

[Route("api/v1/inventory/device-relations")]
[ApiController]
[Tags("Inventory – Device Relations")]
public class DeviceRelationController(DeviceRelationService relationService) : ControllerBase
{
    /// <summary>
    /// Executes the create relation operation.
    /// Core concept: handles the HTTP endpoint contract and delegates business logic to services.
    /// </summary>
    /// <remarks>Potential side effects: may trigger domain workflows that persist state changes.</remarks>
    /// <param name="dto">Request payload containing the input data required for the operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the IActionResult result.</returns>
    [HttpPut("")]
    [Authorize(Roles = nameof(Permissions.DeviceUpdate))]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public async Task<IActionResult> CreateRelation([FromBody] CreateDeviceRelationDto dto, CancellationToken ct)
    {
        var relation = await relationService.CreateRelation(dto, ct);
        return CreatedAtAction(nameof(ListByParentDevice), new { parentDeviceGuid = relation.ParentDeviceGuid }, relation);
    }

    /// <summary>
    /// Executes the list by parent device operation.
    /// Core concept: handles the HTTP endpoint contract and delegates business logic to services.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="parentDeviceGuid">Unique identifier used to target the requested entity.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the IActionResult result.</returns>
    [HttpGet("by-parent/{parentDeviceGuid:guid}")]
    [Authorize(Roles = nameof(Permissions.DeviceRead))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> ListByParentDevice([FromRoute] Guid parentDeviceGuid, CancellationToken ct)
    {
        var relations = await relationService.ListRelationsForParent(parentDeviceGuid, ct);
        return Ok(new { list = relations, total = relations.Count });
    }

    /// <summary>
    /// Executes the delete relation operation.
    /// Core concept: handles the HTTP endpoint contract and delegates business logic to services.
    /// </summary>
    /// <remarks>Potential side effects: may trigger domain workflows that persist state changes.</remarks>
    /// <param name="relationGuid">Unique identifier used to target the requested entity.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the IActionResult result.</returns>
    [HttpDelete("{relationGuid:guid}")]
    [Authorize(Roles = nameof(Permissions.DeviceUpdate))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRelation([FromRoute] Guid relationGuid, CancellationToken ct)
    {
        await relationService.DeleteRelation(relationGuid, ct);
        return NoContent();
    }
}
