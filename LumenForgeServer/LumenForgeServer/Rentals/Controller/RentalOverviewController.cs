using LumenForgeServer.Rentals.Dto.Query;
using LumenForgeServer.Rentals.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace LumenForgeServer.Rentals.Controller;

/// <summary>
/// Read-only HTTP API for querying rental processes, viewing details,
/// and retrieving statistical overviews. No endpoint on this controller
/// modifies any process — all mutations go through <see cref="RentalActionController"/>.
/// </summary>
[Route("api/v1/rentals")]
[ApiController]
[Tags("Rentals – Overview")]
public class RentalOverviewController(
    RentalOverViewService rentalOverViewService,
    RentalAccessService rentalAccessService) : ControllerBase
{
    // ── List & detail ───────────────────────────────────────────────

    /// <summary>
    /// Lists rental processes with optional paging, search, sorting, date-range,
    /// and stage filtering.
    /// </summary>
    /// <param name="query">Input value used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    [HttpGet("")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public async Task<IActionResult> ListProcesses([FromQuery] RentalListQueryDto query, CancellationToken ct)
    {
        var readScope = await rentalAccessService.BuildReadScopeAsync(User, ct);
        if (!readScope.HasAnyScope)
            return Forbid();

        var (list, total) = await rentalOverViewService.ListProcessesAsync(
            query,
            readScope,
            ct);
        return Ok(new { list, total });
    }

    /// <summary>
    /// Returns a single rental process with selectively included details.
    /// Use the <c>include</c> query parameter with a comma-separated list of:
    /// <c>checklists</c>, <c>extensions</c>, <c>damage_reports</c>.
    /// Omitting the parameter returns only the base process and rental data.
    /// </summary>
    /// <param name="processGuid">Public GUID of the process.</param>
    /// <param name="include">Comma-separated include flags (e.g. <c>checklists,extensions</c>).</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("{processGuid:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> GetProcess(
        [FromRoute] Guid processGuid,
        [FromQuery] string? include,
        CancellationToken ct)
    {
        var includes = ParseIncludes(include);
        var readScope = await rentalAccessService.BuildReadScopeAsync(User, ct);
        if (!readScope.HasAnyScope)
            return Forbid();

        var process = await rentalOverViewService.GetProcessAsync(processGuid, includes, readScope, ct);
        return Ok(process);
    }

    /// <summary>
    /// Returns the audit log (action history) for a given process with optional paging,
    /// ordered by date descending.
    /// </summary>
    [HttpGet("{processGuid:guid}/history")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public async Task<IActionResult> GetProcessHistory(
        [FromRoute] Guid processGuid,
        [FromQuery, Range(1, 200)] int limit = 50,
        [FromQuery, Range(0, int.MaxValue)] int offset = 0,
        CancellationToken ct = default)
    {
        var readScope = await rentalAccessService.BuildReadScopeAsync(User, ct);
        if (!readScope.HasAnyScope)
            return Forbid();

        var (list, total) = await rentalOverViewService.GetProcessHistoryAsync(
            processGuid,
            limit,
            offset,
            readScope,
            ct);
        return Ok(new { list, total });
    }

    // ── Statistics ───────────────────────────────────────────────────

    /// <summary>
    /// Returns a high-level statistical overview of all rental processes.
    /// </summary>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    [HttpGet("overview")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Produces("application/json")]
    public async Task<IActionResult> GetOverview(CancellationToken ct)
    {
        var readScope = await rentalAccessService.BuildReadScopeAsync(User, ct);
        if (!readScope.HasAnyScope)
            return Forbid();

        var overview = await rentalOverViewService.GetOverviewAsync(readScope, ct);
        return Ok(overview);
    }

    /// <summary>
    /// Returns a breakdown of recent activity within the specified time window.
    /// Defaults to the last 7 days.
    /// </summary>
    /// <param name="days">Number of days to look back (1–365). Defaults to 7.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("overview/recent")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public async Task<IActionResult> GetRecentActivity(
        [FromQuery] int days = 7, CancellationToken ct = default)
    {
        if (days is < 1 or > 365)
            return BadRequest(new { error = "days must be between 1 and 365." });

        var readScope = await rentalAccessService.BuildReadScopeAsync(User, ct);
        if (!readScope.HasAnyScope)
            return Forbid();

        var activity = await rentalOverViewService.GetRecentActivityAsync(days, readScope, ct);
        return Ok(activity);
    }

    /// <summary>
    /// Returns per-stage process counts as a flat list (useful for chart rendering).
    /// </summary>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    [HttpGet("overview/by-stage")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Produces("application/json")]
    public async Task<IActionResult> GetByStage(CancellationToken ct)
    {
        var readScope = await rentalAccessService.BuildReadScopeAsync(User, ct);
        if (!readScope.HasAnyScope)
            return Forbid();

        var buckets = await rentalOverViewService.GetByStageAsync(readScope, ct);
        return Ok(buckets);
    }

    // ── Helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Executes the parse includes operation.
    /// Core concept: handles the HTTP endpoint contract and delegates business logic to services.
    /// </summary>
    /// <remarks>Potential side effects: may trigger domain workflows that persist state changes.</remarks>
    /// <param name="include">Text input used by this operation.</param>
    /// <returns>The operation result.</returns>
    private static RentalProcessInclude ParseIncludes(string? include)
    {
        if (string.IsNullOrWhiteSpace(include))
            return RentalProcessInclude.None;

        var flags = RentalProcessInclude.None;
        foreach (var part in include.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            flags |= part.ToLowerInvariant() switch
            {
                "checklists" => RentalProcessInclude.Checklists,
                "extensions" => RentalProcessInclude.Extensions,
                "damage_reports" => RentalProcessInclude.DamageReports,
                "all" => RentalProcessInclude.All,
                _ => RentalProcessInclude.None
            };
        }

        return flags;
    }
}
