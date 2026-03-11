using LumenForgeServer.Auth.Domain;
using LumenForgeServer.Common.Exceptions;
using LumenForgeServer.Maintenance.Dto.Command;
using LumenForgeServer.Maintenance.Dto.Query;
using LumenForgeServer.Maintenance.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LumenForgeServer.Maintenance.Controller;

/// <summary>
/// HTTP API for maintenance jobs, tasks, and task logs.
/// </summary>
[Route("api/v1/maintenance")]
[ApiController]
[Authorize]
public class MaintenanceController(MaintenanceService maintenanceService) : ControllerBase
{
    [HttpGet("jobs")]
    [Authorize(Roles = nameof(Permissions.MaintenanceRead))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ListJobs([FromQuery] MaintenanceJobQueryDto query, [FromQuery] string? include, CancellationToken ct)
    {
        var includeFlags = ParseJobIncludes(include);
        var (items, total) = await maintenanceService.ListJobs(query, includeFlags, ct);
        return Ok(new { list = items, total });
    }

    [HttpGet("jobs/{jobGuid:Guid}")]
    [Authorize(Roles = nameof(Permissions.MaintenanceRead))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetJob([FromRoute] Guid jobGuid, [FromQuery] string? include, CancellationToken ct)
    {
        var includeFlags = ParseJobIncludes(include);
        var job = await maintenanceService.GetJob(jobGuid, includeFlags, ct);
        return Ok(job);
    }

    [HttpPut("jobs")]
    [Authorize(Roles = nameof(Permissions.MaintenanceCreate))]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateJob([FromBody] CreateMaintenanceJobDto dto, CancellationToken ct)
    {
        var createdByUserKcId = User.FindFirstValue("sub")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.Identity?.Name
            ?? "unknown-user";

        var job = await maintenanceService.CreateJob(dto, createdByUserKcId, ct);
        return CreatedAtAction(nameof(GetJob), new { jobGuid = job.Guid }, job);
    }

    [HttpPatch("jobs/{jobGuid:Guid}")]
    [Authorize(Roles = nameof(Permissions.MaintenanceUpdate))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateJob([FromRoute] Guid jobGuid, [FromBody] UpdateMaintenanceJobDto dto, CancellationToken ct)
    {
        var job = await maintenanceService.UpdateJob(jobGuid, dto, ct);
        return Ok(job);
    }

    [HttpDelete("jobs/{jobGuid:Guid}")]
    [Authorize(Roles = nameof(Permissions.MaintenanceDelete))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteJob([FromRoute] Guid jobGuid, CancellationToken ct)
    {
        await maintenanceService.DeleteJob(jobGuid, ct);
        return NoContent();
    }

    [HttpGet("jobs/{jobGuid:Guid}/tasks")]
    [Authorize(Roles = nameof(Permissions.MaintenanceRead))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListTasks(
        [FromRoute] Guid jobGuid,
        [FromQuery] MaintenanceTaskQueryDto query,
        [FromQuery] string? include,
        CancellationToken ct)
    {
        var includeFlags = ParseTaskIncludes(include);
        var (tasks, total) = await maintenanceService.ListTasks(jobGuid, query.Limit, query.Offset, includeFlags, ct);
        return Ok(new { list = tasks, total });
    }

    [HttpPost("jobs/{jobGuid:Guid}/tasks")]
    [Authorize(Roles = nameof(Permissions.MaintenanceCreate))]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateTask([FromRoute] Guid jobGuid, [FromBody] CreateMaintenanceTaskDto dto, CancellationToken ct)
    {
        var task = await maintenanceService.CreateTask(jobGuid, dto, ct);
        return CreatedAtAction(nameof(ListTasks), new { jobGuid }, task);
    }

    [HttpPatch("jobs/{jobGuid:Guid}/tasks/{taskGuid:Guid}")]
    [Authorize(Roles = nameof(Permissions.MaintenanceUpdate))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTask([FromRoute] Guid jobGuid, [FromRoute] Guid taskGuid, [FromBody] UpdateMaintenanceTaskDto dto, CancellationToken ct)
    {
        var task = await maintenanceService.UpdateTask(jobGuid, taskGuid, dto, ct);
        return Ok(task);
    }

    [HttpDelete("jobs/{jobGuid:Guid}/tasks/{taskGuid:Guid}")]
    [Authorize(Roles = nameof(Permissions.MaintenanceDelete))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTask([FromRoute] Guid jobGuid, [FromRoute] Guid taskGuid, CancellationToken ct)
    {
        await maintenanceService.DeleteTask(jobGuid, taskGuid, ct);
        return NoContent();
    }

    /// <summary>
    /// Adds a QR-scanned device to a maintenance job's affected devices.
    /// Idempotent — scanning the same device more than once is safe.
    /// </summary>
    /// <remarks>
    /// Expected mobile flow: scan device QR → POST here → binding is created → job view is refreshed.
    /// </remarks>
    [HttpPost("jobs/{jobGuid:Guid}/devices/scan")]
    [Authorize(Roles = nameof(Permissions.MaintenanceUpdate))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> ScanDeviceForJob(
        [FromRoute] Guid jobGuid,
        [FromBody] ScanDeviceDto dto,
        CancellationToken ct)
    {
        var job = await maintenanceService.ScanDeviceForJob(jobGuid, dto.DeviceGuid, ct);
        return Ok(job);
    }

    /// <summary>
    /// Adds a QR-scanned device to a maintenance task's affected devices.
    /// Idempotent — scanning the same device more than once is safe.
    /// </summary>
    /// <remarks>
    /// Expected mobile flow: scan device QR → POST here → task view is refreshed.
    /// </remarks>
    [HttpPost("jobs/{jobGuid:Guid}/tasks/{taskGuid:Guid}/devices/scan")]
    [Authorize(Roles = nameof(Permissions.MaintenanceUpdate))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> ScanDeviceForTask(
        [FromRoute] Guid jobGuid,
        [FromRoute] Guid taskGuid,
        [FromBody] ScanDeviceDto dto,
        CancellationToken ct)
    {
        var task = await maintenanceService.ScanDeviceForTask(jobGuid, taskGuid, dto.DeviceGuid, ct);
        return Ok(task);
    }

    private static MaintenanceJobInclude ParseJobIncludes(string? include)
    {
        if (string.IsNullOrWhiteSpace(include))
        {
            return MaintenanceJobInclude.None;
        }

        MaintenanceJobInclude flags = MaintenanceJobInclude.None;
        var values = include.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var value in values)
        {
            if (!Enum.TryParse<MaintenanceJobInclude>(value, true, out var parsed))
            {
                throw new ValidationException(
                    $"Invalid include value '{value}'.",
                    new Dictionary<string, string[]>
                    {
                        ["include"] = [$"Unsupported job include '{value}'. Allowed values: {string.Join(", ", Enum.GetNames<MaintenanceJobInclude>())}"]
                    });
            }

            flags |= parsed;
        }

        return flags;
    }

    private static MaintenanceTaskInclude ParseTaskIncludes(string? include)
    {
        if (string.IsNullOrWhiteSpace(include))
        {
            return MaintenanceTaskInclude.None;
        }

        MaintenanceTaskInclude flags = MaintenanceTaskInclude.None;
        var values = include.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var value in values)
        {
            if (!Enum.TryParse<MaintenanceTaskInclude>(value, true, out var parsed))
            {
                throw new ValidationException(
                    $"Invalid include value '{value}'.",
                    new Dictionary<string, string[]>
                    {
                        ["include"] = [$"Unsupported task include '{value}'. Allowed values: {string.Join(", ", Enum.GetNames<MaintenanceTaskInclude>())}"]
                    });
            }

            flags |= parsed;
        }

        return flags;
    }
}
