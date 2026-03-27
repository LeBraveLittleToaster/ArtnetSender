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
