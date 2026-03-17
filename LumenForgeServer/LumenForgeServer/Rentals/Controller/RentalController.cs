using LumenForgeServer.Auth.Domain;
using LumenForgeServer.Common.Exceptions;
using LumenForgeServer.Rentals.Dto.Command;
using LumenForgeServer.Rentals.Dto.Query;
using LumenForgeServer.Rentals.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LumenForgeServer.Rentals.Controller;

/// <summary>
/// HTTP API for rental lifecycle management, stock-binding conflict checks, and checklists.
/// </summary>
/// <remarks>
/// Routes are under <c>api/v1/rentals</c>.
/// </remarks>
[Route("api/v1/rentals")]
[ApiController]
[Authorize]
public class RentalController(RentalService rentalService, ChecklistService checklistService) : ControllerBase
{
    /// <summary>
    /// Lists rentals with optional paging, search, and include flags.
    /// </summary>
    /// <remarks>
    /// Example: <c>GET /api/v1/rentals?limit=25&amp;offset=0&amp;include=Items,Events</c>
    /// </remarks>
    [HttpGet("")]
    [Authorize(Roles = nameof(Permissions.RentalRead))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public async Task<IActionResult> ListRentals(
        [FromQuery] RentalQueryDto query,
        [FromQuery] string? include,
        CancellationToken ct)
    {
        var includeFlags = ParseIncludes(include);
        var (items, total) = await rentalService.ListRentals(query, includeFlags, ct);
        return Ok(new { list = items, total });
    }

    [HttpGet("{rentalGuid:Guid}")]
    [Authorize(Roles = nameof(Permissions.RentalRead))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> GetRental(
        [FromRoute] Guid rentalGuid,
        [FromQuery] string? include,
        CancellationToken ct)
    {
        var includeFlags = ParseIncludes(include);
        var rental = await rentalService.GetRental(rentalGuid, includeFlags, ct);
        return Ok(rental);
    }

    [HttpPut("")]
    [Authorize(Roles = nameof(Permissions.RentalCreate))]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> CreateRental([FromBody] CreateRentalDto dto, CancellationToken ct)
    {
        var customerUserId = User.FindFirstValue("sub")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.Identity?.Name
            ?? "unknown-user";

        var rental = await rentalService.CreateRental(dto, customerUserId, ct);
        return CreatedAtAction(nameof(GetRental), new { rentalGuid = rental.Uuid }, rental);
    }

    [HttpPatch("{rentalGuid:Guid}")]
    [Authorize(Roles = nameof(Permissions.RentalUpdate))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> UpdateRental(
        [FromRoute] Guid rentalGuid,
        [FromBody] UpdateRentalDto dto,
        CancellationToken ct)
    {
        var actorUserId = User.FindFirstValue("sub")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.Identity?.Name
            ?? "unknown-user";

        var rental = await rentalService.UpdateRental(rentalGuid, dto, actorUserId, ct);
        return Ok(rental);
    }

    [HttpDelete("{rentalGuid:Guid}")]
    [Authorize(Roles = nameof(Permissions.RentalDelete))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> DeleteRental([FromRoute] Guid rentalGuid, CancellationToken ct)
    {
        await rentalService.DeleteRental(rentalGuid, ct);
        return NoContent();
    }

    /// <summary>
    /// Returns all stock bindings that overlap the proposed rental window for a given device.
    /// Use this before submitting a rental item to check whether the device is already reserved.
    /// </summary>
    /// <remarks>
    /// Example: <c>GET /api/v1/rentals/conflicts?device_guid=...&amp;start=2025-01-01T00:00:00Z&amp;end=2025-01-07T00:00:00Z</c>
    /// </remarks>
    [HttpGet("conflicts")]
    [Authorize(Roles = nameof(Permissions.RentalRead))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> ListConflicts([FromQuery] RentalConflictQueryDto query, CancellationToken ct)
    {
        var (items, total) = await rentalService.ListConflicts(query, ct);
        return Ok(new { list = items, total });
    }

    /// <summary>
    /// Lists all checklists for a rental ordered by generation time.
    /// Each checklist includes its items and partial-completion counters.
    /// </summary>
    [HttpGet("{rentalGuid:Guid}/checklists")]
    [Authorize(Roles = nameof(Permissions.RentalRead))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> ListChecklists(
        [FromRoute] Guid rentalGuid,
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0,
        CancellationToken ct = default)
    {
        var (items, total) = await checklistService.ListChecklists(rentalGuid, limit, offset, ct);
        return Ok(new { list = items, total });
    }

    /// <summary>
    /// Generates a new checklist for a rental.
    /// PICKUP checklists are seeded from the rental's approved line items.
    /// DROPOFF checklists mirror the items of a referenced PICKUP checklist.
    /// </summary>
    [HttpPost("{rentalGuid:Guid}/checklists/generate")]
    [Authorize(Roles = nameof(Permissions.RentalUpdate))]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> GenerateChecklist(
        [FromRoute] Guid rentalGuid,
        [FromBody] GenerateChecklistDto dto,
        CancellationToken ct)
    {
        var staffUserId = User.FindFirstValue("sub")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.Identity?.Name
            ?? "unknown-user";

        var checklist = await checklistService.GenerateChecklist(rentalGuid, dto, staffUserId, ct);
        return CreatedAtAction(nameof(GetChecklist), new { rentalGuid, checklistGuid = checklist.Uuid }, checklist);
    }

    /// <summary>
    /// Returns a single checklist with all its inspection items.
    /// </summary>
    [HttpGet("{rentalGuid:Guid}/checklists/{checklistGuid:Guid}")]
    [Authorize(Roles = nameof(Permissions.RentalRead))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> GetChecklist(
        [FromRoute] Guid rentalGuid,
        [FromRoute] Guid checklistGuid,
        CancellationToken ct)
    {
        var checklist = await checklistService.GetChecklist(rentalGuid, checklistGuid, ct);
        return Ok(checklist);
    }

    /// <summary>
    /// Submits an inspection result for a single checklist item, marking it as checked.
    /// The parent checklist must not yet be signed.
    /// Checklists may be partially complete — only submitted items have <c>is_checked = true</c>.
    /// </summary>
    [HttpPatch("{rentalGuid:Guid}/checklists/{checklistGuid:Guid}/items/{itemGuid:Guid}")]
    [Authorize(Roles = nameof(Permissions.RentalUpdate))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> UpdateChecklistItem(
        [FromRoute] Guid rentalGuid,
        [FromRoute] Guid checklistGuid,
        [FromRoute] Guid itemGuid,
        [FromBody] UpdateChecklistItemDto dto,
        CancellationToken ct)
    {
        var item = await checklistService.UpdateChecklistItem(rentalGuid, checklistGuid, itemGuid, dto, ct);
        return Ok(item);
    }

    /// <summary>
    /// Signs and finalises a checklist, making it immutable.
    /// Signing is allowed even if some items are still unchecked (partial-completion is valid).
    /// </summary>
    [HttpPost("{rentalGuid:Guid}/checklists/{checklistGuid:Guid}/sign")]
    [Authorize(Roles = nameof(Permissions.RentalUpdate))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> SignChecklist(
        [FromRoute] Guid rentalGuid,
        [FromRoute] Guid checklistGuid,
        [FromBody] SignChecklistDto dto,
        CancellationToken ct)
    {
        var staffUserId = User.FindFirstValue("sub")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.Identity?.Name
            ?? "unknown-user";

        var checklist = await checklistService.SignChecklist(rentalGuid, checklistGuid, dto, staffUserId, ct);
        return Ok(checklist);
    }

    /// <summary>
    /// Looks up the checklist item for a QR-scanned device.
    /// Returns the current item state so the mobile app can pre-populate the inspection form.
    /// The actual inspection result is submitted separately via
    /// <c>PATCH .../items/{itemGuid}</c>.
    /// </summary>
    /// <remarks>
    /// Returns <c>404</c> when the scanned device has no stock binding linked to an approved
    /// rental item on this checklist, or when the checklist itself does not exist.
    /// Returns <c>400</c> when the checklist has already been signed.
    /// </remarks>
    [HttpGet("{rentalGuid:Guid}/checklists/{checklistGuid:Guid}/scan")]
    [Authorize(Roles = nameof(Permissions.RentalRead))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> ScanDeviceOnChecklist(
        [FromRoute] Guid rentalGuid,
        [FromRoute] Guid checklistGuid,
        [FromQuery] Guid deviceGuid,
        CancellationToken ct)
    {
        var item = await checklistService.ScanDeviceOnChecklist(rentalGuid, checklistGuid, deviceGuid, ct);
        return Ok(item);
    }

    /// <summary>
    /// Lists all available rental status types.
    /// </summary>
    [HttpGet("statuses")]
    [Authorize(Roles = nameof(Permissions.RentalStatusRead))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Produces("application/json")]
    public async Task<IActionResult> ListRentalStatuses(CancellationToken ct)
    {
        var statuses = await rentalService.ListRentalStatuses(ct);
        return Ok(new { list = statuses, total = statuses.Count });
    }

    /// <summary>
    /// Returns the current and allowed status transitions for a rental.
    /// </summary>
    [HttpGet("{rentalGuid:Guid}/transitions")]
    [Authorize(Roles = nameof(Permissions.RentalRead))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> ListAllowedTransitions([FromRoute] Guid rentalGuid, CancellationToken ct)
    {
        var (current, allowed) = await rentalService.ListAllowedTransitions(rentalGuid, ct);
        return Ok(new { current, allowed });
    }

    /// <summary>
    /// Transitions the rental to a different status.
    /// Only allowed statuses can be transitioned to.
    /// </summary>
    [HttpPost("{rentalGuid:Guid}/transitions")]
    [Authorize(Roles = nameof(Permissions.RentalUpdate))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> TransitionRentalStatus(
        [FromRoute] Guid rentalGuid,
        [FromBody] TransitionRentalStatusDto dto,
        CancellationToken ct)
    {
        var actorUserId = User.FindFirstValue("sub")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.Identity?.Name
            ?? "unknown-user";

        var rental = await rentalService.TransitionRentalStatus(rentalGuid, dto.TargetStatus, actorUserId, ct);
        return Ok(rental);
    }

    private static RentalInclude ParseIncludes(string? include)
    {
        if (string.IsNullOrWhiteSpace(include))
        {
            return RentalInclude.None;
        }

        RentalInclude flags = RentalInclude.None;
        var values = include.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var value in values)
        {
            if (!Enum.TryParse<RentalInclude>(value, true, out var parsed))
            {
                throw new ValidationException(
                    $"Invalid include value '{value}'.",
                    new Dictionary<string, string[]>
                    {
                        ["include"] = [$"Unsupported include '{value}'. Allowed values: {string.Join(", ", Enum.GetNames<RentalInclude>())}"]
                    });
            }

            flags |= parsed;
        }

        return flags;
    }
}
