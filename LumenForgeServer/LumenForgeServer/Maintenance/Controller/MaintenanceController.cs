using LumenForgeServer.Auth.Domain;
using LumenForgeServer.Maintenance.Dto.Command;
using LumenForgeServer.Maintenance.Dto.Query;
using LumenForgeServer.Maintenance.Dto.View;
using LumenForgeServer.Maintenance.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LumenForgeServer.Maintenance.Controller;

/// <summary>
/// HTTP API for the maintenance module.
/// </summary>
/// <remarks>
/// Routes are under <c>api/v1/maintenance</c> and require authenticated access.
/// </remarks>
[Route("api/v1/maintenance")]
[ApiController]
[Authorize]
public class MaintenanceController(
    MaintenanceService maintenanceService,
    MaintenanceStatusService statusService) : ControllerBase
{
    // ── Backlog statuses ─────────────────────────────────────────────────────

    /// <summary>Lists all maintenance backlog statuses.</summary>
    [HttpGet("statuses")]
    [Authorize(Roles = nameof(Permissions.MaintenanceRead))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public async Task<IActionResult> ListStatuses([FromQuery] MaintenanceQueryDto query, CancellationToken ct)
    {
        var statuses = await statusService.ListStatuses(query.Search, query.Limit, query.Offset, ct);
        return Ok(statuses);
    }

    /// <summary>Returns a single maintenance status by UUID.</summary>
    [HttpGet("statuses/{uuid:Guid}")]
    [Authorize(Roles = nameof(Permissions.MaintenanceRead))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> GetStatus([FromRoute] Guid uuid, CancellationToken ct)
    {
        var status = await statusService.GetStatus(uuid, ct);
        return Ok(status);
    }

    /// <summary>Creates a new maintenance backlog status.</summary>
    [HttpPut("statuses")]
    [Authorize(Roles = nameof(Permissions.MaintenanceCreate))]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Produces("application/json")]
    public async Task<IActionResult> CreateStatus([FromBody] CreateMaintenanceStatusDto dto, CancellationToken ct)
    {
        var status = await statusService.CreateStatus(dto, ct);
        return CreatedAtAction(nameof(GetStatus), new { uuid = status.Uuid }, status);
    }

    /// <summary>Partially updates a maintenance backlog status.</summary>
    [HttpPatch("statuses/{uuid:Guid}")]
    [Authorize(Roles = nameof(Permissions.MaintenanceUpdate))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Produces("application/json")]
    public async Task<IActionResult> UpdateStatus([FromRoute] Guid uuid, [FromBody] UpdateMaintenanceStatusDto dto, CancellationToken ct)
    {
        var status = await statusService.UpdateStatus(uuid, dto, ct);
        return Ok(status);
    }

    /// <summary>Deletes a maintenance backlog status. Fails if any backlogs reference it.</summary>
    [HttpDelete("statuses/{uuid:Guid}")]
    [Authorize(Roles = nameof(Permissions.MaintenanceDelete))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Produces("application/json")]
    public async Task<IActionResult> DeleteStatus([FromRoute] Guid uuid, CancellationToken ct)
    {
        await statusService.DeleteStatus(uuid, ct);
        return NoContent();
    }

    // ── Backlog entries ───────────────────────────────────────────────────────

    /// <summary>
    /// Lists maintenance backlog entries with optional paging, search, status filter and resolved filter.
    /// </summary>
    [HttpGet("backlogs")]
    [Authorize(Roles = nameof(Permissions.MaintenanceRead))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public async Task<IActionResult> ListBacklogs([FromQuery] MaintenanceQueryDto query, CancellationToken ct)
    {
        var (items, total) = await maintenanceService.ListBacklogs(query, ct);
        return Ok(new { list = items, total });
    }

    /// <summary>Returns a single backlog entry by UUID.</summary>
    [HttpGet("backlogs/{uuid:Guid}")]
    [Authorize(Roles = nameof(Permissions.MaintenanceRead))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> GetBacklog([FromRoute] Guid uuid, CancellationToken ct)
    {
        var backlog = await maintenanceService.GetBacklog(uuid, ct);
        return Ok(backlog);
    }

    /// <summary>Returns all backlog entries linked to a specific device.</summary>
    [HttpGet("devices/{deviceUuid:Guid}/backlogs")]
    [Authorize(Roles = nameof(Permissions.MaintenanceRead))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> GetBacklogsByDevice([FromRoute] Guid deviceUuid, CancellationToken ct)
    {
        var items = await maintenanceService.GetBacklogsByDevice(deviceUuid, ct);
        return Ok(items);
    }

    /// <summary>Creates a new maintenance backlog entry.</summary>
    [HttpPut("backlogs")]
    [Authorize(Roles = nameof(Permissions.MaintenanceCreate))]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> CreateBacklog([FromBody] CreateMaintenanceBacklogDto dto, CancellationToken ct)
    {
        var backlog = await maintenanceService.CreateBacklog(dto, ct);
        return CreatedAtAction(nameof(GetBacklog), new { uuid = backlog.Uuid }, backlog);
    }

    /// <summary>Partially updates a backlog entry, including resolving it.</summary>
    [HttpPatch("backlogs/{uuid:Guid}")]
    [Authorize(Roles = nameof(Permissions.MaintenanceUpdate))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> UpdateBacklog([FromRoute] Guid uuid, [FromBody] UpdateMaintenanceBacklogDto dto, CancellationToken ct)
    {
        var backlog = await maintenanceService.UpdateBacklog(uuid, dto, ct);
        return Ok(backlog);
    }

    /// <summary>Deletes a backlog entry.</summary>
    [HttpDelete("backlogs/{uuid:Guid}")]
    [Authorize(Roles = nameof(Permissions.MaintenanceDelete))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> DeleteBacklog([FromRoute] Guid uuid, CancellationToken ct)
    {
        await maintenanceService.DeleteBacklog(uuid, ct);
        return NoContent();
    }
}
