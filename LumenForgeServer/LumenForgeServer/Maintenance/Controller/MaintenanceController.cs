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
/// <remarks>
/// <para>Jobs group related repair/inspection work and can reference devices, other jobs, and rentals.</para>
/// <para>Tasks are individual work items inside a job. Each task has its own status lifecycle and log.</para>
/// </remarks>
[Route("api/v1/maintenance")]
[ApiController]
[Authorize]
[Tags("Maintenance")]
public class MaintenanceController(MaintenanceService maintenanceService) : ControllerBase
{
    /// <summary>
    /// Lists maintenance jobs with optional paging, search, status filtering, and relation includes.
    /// </summary>
    /// <remarks>
    /// Pass <c>include=Devices,Tasks,Logs,RelatedJobs,RelatedRental</c> (comma-separated)
    /// to embed related entities in the response.
    /// </remarks>
    /// <param name="query">Paging, search, and status filter parameters.</param>
    /// <param name="include">Comma-separated include flags (Devices, Tasks, Logs, RelatedJobs, RelatedRental).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 200 response with job results.</returns>
    [HttpGet("jobs")]
    [Authorize(Roles = nameof(Permissions.MaintenanceRead))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public async Task<IActionResult> ListJobs([FromQuery] MaintenanceJobQueryDto query, [FromQuery] string? include, CancellationToken ct)
    {
        var includeFlags = ParseJobIncludes(include);
        var (items, total) = await maintenanceService.ListJobs(query, includeFlags, ct);
        return Ok(new { list = items, total });
    }

    /// <summary>
    /// Retrieves a single maintenance job by its GUID.
    /// </summary>
    /// <param name="jobGuid">Unique job identifier.</param>
    /// <param name="include">Comma-separated include flags (Devices, Tasks, Logs, RelatedJobs, RelatedRental).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 200 response with the job payload.</returns>
    [HttpGet("jobs/{jobGuid:Guid}")]
    [Authorize(Roles = nameof(Permissions.MaintenanceRead))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> GetJob([FromRoute] Guid jobGuid, [FromQuery] string? include, CancellationToken ct)
    {
        var includeFlags = ParseJobIncludes(include);
        var job = await maintenanceService.GetJob(jobGuid, includeFlags, ct);
        return Ok(job);
    }

    /// <summary>
    /// Creates a new maintenance job.
    /// </summary>
    /// <remarks>
    /// The actor is inferred from the JWT <c>sub</c> claim.
    /// Referenced devices and related jobs must exist; inline tasks are created atomically.
    /// </remarks>
    /// <param name="dto">Job creation payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 201 response with the created job.</returns>
    [HttpPut("jobs")]
    [Authorize(Roles = nameof(Permissions.MaintenanceCreate))]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> CreateJob([FromBody] CreateMaintenanceJobDto dto, CancellationToken ct)
    {
        var createdByUserKcId = User.FindFirstValue("sub")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.Identity?.Name
            ?? "unknown-user";

        var job = await maintenanceService.CreateJob(dto, createdByUserKcId, ct);
        return CreatedAtAction(nameof(GetJob), new { jobGuid = job.Guid }, job);
    }

    /// <summary>
    /// Partially updates a maintenance job (name, description, or status).
    /// </summary>
    /// <param name="jobGuid">Job to update.</param>
    /// <param name="dto">Fields to change.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 200 response with the updated job.</returns>
    [HttpPatch("jobs/{jobGuid:Guid}")]
    [Authorize(Roles = nameof(Permissions.MaintenanceUpdate))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public async Task<IActionResult> UpdateJob([FromRoute] Guid jobGuid, [FromBody] UpdateMaintenanceJobDto dto, CancellationToken ct)
    {
        var job = await maintenanceService.UpdateJob(jobGuid, dto, ct);
        return Ok(job);
    }

    /// <summary>
    /// Permanently deletes a maintenance job and all its tasks and logs.
    /// </summary>
    /// <param name="jobGuid">Job to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 204 response when deleted successfully.</returns>
    [HttpDelete("jobs/{jobGuid:Guid}")]
    [Authorize(Roles = nameof(Permissions.MaintenanceDelete))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteJob([FromRoute] Guid jobGuid, CancellationToken ct)
    {
        await maintenanceService.DeleteJob(jobGuid, ct);
        return NoContent();
    }

    /// <summary>
    /// Lists tasks for a specific maintenance job with optional paging and includes.
    /// </summary>
    /// <param name="jobGuid">Parent job identifier.</param>
    /// <param name="query">Paging parameters.</param>
    /// <param name="include">Comma-separated include flags (Devices, Logs).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 200 response with task results.</returns>
    [HttpGet("jobs/{jobGuid:Guid}/tasks")]
    [Authorize(Roles = nameof(Permissions.MaintenanceRead))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
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

    /// <summary>
    /// Creates a new task within a maintenance job.
    /// </summary>
    /// <param name="jobGuid">Parent job identifier.</param>
    /// <param name="dto">Task creation payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 201 response with the created task.</returns>
    [HttpPost("jobs/{jobGuid:Guid}/tasks")]
    [Authorize(Roles = nameof(Permissions.MaintenanceCreate))]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> CreateTask([FromRoute] Guid jobGuid, [FromBody] CreateMaintenanceTaskDto dto, CancellationToken ct)
    {
        var task = await maintenanceService.CreateTask(jobGuid, dto, ct);
        return CreatedAtAction(nameof(ListTasks), new { jobGuid }, task);
    }

    /// <summary>
    /// Partially updates a task (description, status, or assignee).
    /// </summary>
    /// <param name="jobGuid">Parent job identifier.</param>
    /// <param name="taskGuid">Task to update.</param>
    /// <param name="dto">Fields to change.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 200 response with the updated task.</returns>
    [HttpPatch("jobs/{jobGuid:Guid}/tasks/{taskGuid:Guid}")]
    [Authorize(Roles = nameof(Permissions.MaintenanceUpdate))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public async Task<IActionResult> UpdateTask([FromRoute] Guid jobGuid, [FromRoute] Guid taskGuid, [FromBody] UpdateMaintenanceTaskDto dto, CancellationToken ct)
    {
        var task = await maintenanceService.UpdateTask(jobGuid, taskGuid, dto, ct);
        return Ok(task);
    }

    /// <summary>
    /// Permanently deletes a task from a maintenance job.
    /// </summary>
    /// <param name="jobGuid">Parent job identifier.</param>
    /// <param name="taskGuid">Task to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 204 response when deleted successfully.</returns>
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
    /// <param name="jobGuid">Unique identifier used to target the requested entity.</param>
    /// <param name="dto">Request payload containing the input data required for the operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
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
    /// <param name="jobGuid">Unique identifier used to target the requested entity.</param>
    /// <param name="taskGuid">Unique identifier used to target the requested entity.</param>
    /// <param name="dto">Request payload containing the input data required for the operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
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

    /// <summary>
    /// Lists immutable log entries for a specific task, with optional paging.
    /// </summary>
    /// <param name="jobGuid">Parent job identifier.</param>
    /// <param name="taskGuid">Task whose logs to retrieve.</param>
    /// <param name="limit">Maximum results (1–200, default 50).</param>
    /// <param name="offset">Records to skip (default 0).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 200 response with log entry results.</returns>
    [HttpGet("jobs/{jobGuid:Guid}/tasks/{taskGuid:Guid}/logs")]
    [Authorize(Roles = nameof(Permissions.MaintenanceRead))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> ListTaskLogs(
        [FromRoute] Guid jobGuid,
        [FromRoute] Guid taskGuid,
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0,
        CancellationToken ct = default)
    {
        var (items, total) = await maintenanceService.ListTaskLogs(jobGuid, taskGuid, limit, offset, ct);
        return Ok(new { list = items, total });
    }

    /// <summary>
    /// Appends an immutable log entry to a task.
    /// </summary>
    /// <remarks>
    /// If <c>status_after</c> is provided and differs from the current task status,
    /// the task status is updated as a side-effect.
    /// </remarks>
    /// <param name="jobGuid">Parent job identifier.</param>
    /// <param name="taskGuid">Task to append to.</param>
    /// <param name="dto">Log entry payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 201 response with the created log entry.</returns>
    [HttpPost("jobs/{jobGuid:Guid}/tasks/{taskGuid:Guid}/logs")]
    [Authorize(Roles = nameof(Permissions.MaintenanceUpdate))]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> AddTaskLog(
        [FromRoute] Guid jobGuid,
        [FromRoute] Guid taskGuid,
        [FromBody] CreateMaintenanceLogEntryDto dto,
        CancellationToken ct)
    {
        var log = await maintenanceService.AddTaskLog(jobGuid, taskGuid, dto, ct);
        return CreatedAtAction(nameof(ListTaskLogs), new { jobGuid, taskGuid }, log);
    }

    /// <summary>
    /// Executes the parse job includes operation.
    /// Core concept: handles the HTTP endpoint contract and delegates business logic to services.
    /// </summary>
    /// <remarks>Potential side effects: may trigger domain workflows that persist state changes.</remarks>
    /// <param name="include">Text input used by this operation.</param>
    /// <returns>The operation result.</returns>
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

    /// <summary>
    /// Executes the parse task includes operation.
    /// Core concept: handles the HTTP endpoint contract and delegates business logic to services.
    /// </summary>
    /// <remarks>Potential side effects: may trigger domain workflows that persist state changes.</remarks>
    /// <param name="include">Text input used by this operation.</param>
    /// <returns>The operation result.</returns>
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
