using LumenForgeServer.Auth.Domain;
using LumenForgeServer.Auth.Domain.Session;
using LumenForgeServer.Common.Exceptions;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Dto.Query;
using LumenForgeServer.Rentals.Dto.View;
using LumenForgeServer.Rentals.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NodaTime;
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
    IRentalProcessRepository repository,
    IKeycloakUser keycloakUser) : ControllerBase
{
    // ── Terminal stages used for statistics ──────────────────────────

    private static readonly HashSet<RentalStage> TerminalStages =
    [
        RentalStage.Completed,
        RentalStage.Cancelled,
        RentalStage.Scrapped
    ];

    // ── List & detail ───────────────────────────────────────────────

    /// <summary>
    /// Lists rental processes with optional paging, search, sorting, date-range,
    /// and stage filtering.
    /// </summary>
    [HttpGet("")]
    [Authorize(Roles = nameof(Permissions.RentalRead))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public async Task<IActionResult> ListProcesses([FromQuery] RentalListQueryDto query, CancellationToken ct)
    {
        var (items, total) = await repository.ListAsync(query, ct);

        var views = items.Select(RentalProcessSummaryView.FromEntity).ToList();
        return Ok(new { list = views, total });
    }

    /// <summary>
    /// Lists only the authenticated user's own rental processes.
    /// Does not require the <c>RentalRead</c> permission — any authenticated
    /// user may view their own processes.
    /// </summary>
    [HttpGet("my")]
    [Authorize(Policy = nameof(Policy.RentalReadOrOwnProcesses))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [Produces("application/json")]
    public async Task<IActionResult> ListMyProcesses([FromQuery] RentalListQueryDto query, CancellationToken ct)
    {
        var callerKcId = keycloakUser.UserId
            ?? throw new UnauthorizedAccessException("Unable to resolve caller identity.");

        var scoped = query with { OwnerKcId = callerKcId };
        var (items, total) = await repository.ListAsync(scoped, ct);

        var views = items.Select(RentalProcessSummaryView.FromEntity).ToList();
        return Ok(new { list = views, total });
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
    [Authorize(Roles = nameof(Permissions.RentalRead))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> GetProcess(
        [FromRoute] Guid processGuid,
        [FromQuery] string? include,
        CancellationToken ct)
    {
        var includes = ParseIncludes(include);

        var process = includes == RentalProcessInclude.None
            ? await repository.GetByGuidAsync(processGuid, ct)
            : await repository.GetByGuidWithIncludesAsync(processGuid, includes, ct);

        if (process is null)
            throw new NotFoundException($"Process instance '{processGuid}' not found.");

        return Ok(RentalProcessView.FromEntity(process, includes));
    }

    /// <summary>
    /// Returns the audit log (action history) for a given process with optional paging,
    /// ordered by date descending.
    /// </summary>
    /// <param name="processGuid">Public GUID of the process.</param>
    /// <param name="limit">Maximum number of log entries to return (1–200, default 50).</param>
    /// <param name="offset">Number of entries to skip (default 0).</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("{processGuid:guid}/history")]
    [Authorize(Roles = nameof(Permissions.RentalRead))]
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
        _ = await repository.GetByGuidAsync(processGuid, ct)
            ?? throw new NotFoundException($"Process instance '{processGuid}' not found.");

        var (logs, total) = await repository.GetActionLogsByProcessGuidAsync(processGuid, limit, offset, ct);
        return Ok(new { list = logs.Select(RentalActionLogView.FromEntity).ToList(), total });
    }

    // ── Statistics ───────────────────────────────────────────────────

    /// <summary>
    /// Returns a high-level statistical overview of all rental processes.
    /// </summary>
    [HttpGet("overview")]
    [Authorize(Roles = nameof(Permissions.RentalRead))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Produces("application/json")]
    public async Task<IActionResult> GetOverview(CancellationToken ct)
    {
        var byStage = await repository.CountByStageAsync(ct);
        var totalProcesses = byStage.Values.Sum();
        var terminalCount = byStage
            .Where(kv => TerminalStages.Contains(kv.Key))
            .Sum(kv => kv.Value);

        var overview = new RentalOverviewDto
        {
            TotalProcesses = totalProcesses,
            ByStage = byStage,
            ActiveCount = totalProcesses - terminalCount,
            TerminalCount = terminalCount,
            TotalDamageReports = await repository.CountDamageReportsAsync(ct),
            TotalExtensionRequests = await repository.CountExtensionsAsync(ct),
            PendingExtensions = await repository.CountPendingExtensionsAsync(ct),
            TotalActionLogs = await repository.CountActionLogsAsync(ct)
        };

        return Ok(overview);
    }

    /// <summary>
    /// Returns a breakdown of recent activity within the specified time window.
    /// Defaults to the last 7 days.
    /// </summary>
    /// <param name="days">Number of days to look back (1–365). Defaults to 7.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet("overview/recent")]
    [Authorize(Roles = nameof(Permissions.RentalRead))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public async Task<IActionResult> GetRecentActivity(
        [FromQuery] int days = 7, CancellationToken ct = default)
    {
        if (days is < 1 or > 365)
            return BadRequest(new { error = "days must be between 1 and 365." });

        var since = SystemClock.Instance.GetCurrentInstant() - Duration.FromDays(days);

        var activity = new RentalRecentActivityDto
        {
            ProcessesCreated = await repository.CountProcessesCreatedSinceAsync(since, ct),
            ProcessesCompleted = await repository.CountProcessesReachedStageSinceAsync(
                RentalStage.Completed, since, ct),
            ProcessesCancelled = await repository.CountProcessesReachedStageSinceAsync(
                RentalStage.Cancelled, since, ct),
            ActionsPerformed = await repository.CountActionLogsSinceAsync(since, ct),
            DamagesReported = await repository.CountDamageReportsSinceAsync(since, ct),
            WindowDays = days
        };

        return Ok(activity);
    }

    /// <summary>
    /// Returns per-stage process counts as a flat list (useful for chart rendering).
    /// </summary>
    [HttpGet("overview/by-stage")]
    [Authorize(Roles = nameof(Permissions.RentalRead))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Produces("application/json")]
    public async Task<IActionResult> GetByStage(CancellationToken ct)
    {
        var byStage = await repository.CountByStageAsync(ct);

        var buckets = Enum.GetValues<RentalStage>()
            .Select(stage => new StageBucketDto
            {
                Stage = stage,
                Count = byStage.GetValueOrDefault(stage, 0)
            })
            .ToList();

        return Ok(buckets);
    }

    // ── Helpers ──────────────────────────────────────────────────────

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
