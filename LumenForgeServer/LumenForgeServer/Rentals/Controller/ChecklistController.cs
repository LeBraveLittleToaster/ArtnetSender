using LumenForgeServer.Auth.Domain;
using LumenForgeServer.Rentals.Dto.Command;
using LumenForgeServer.Rentals.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LumenForgeServer.Rentals.Controller;

/// <summary>
/// HTTP API for rental checklist generation, inspection updates, signing, and device scans.
/// </summary>
/// <remarks>
/// Routes are under <c>api/v1/rentals/{rentalGuid}/checklists</c>.
/// </remarks>
[Route("api/v1/rentals/{rentalGuid:Guid}/checklists")]
[ApiController]
[Authorize]
public class ChecklistController(ChecklistService checklistService) : ControllerBase
{
    /// <summary>
    /// Lists all checklists for a rental ordered by generation time.
    /// Each checklist includes its items and partial-completion counters.
    /// </summary>
    [HttpGet("")]
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
    [HttpPost("generate")]
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
    [HttpGet("{checklistGuid:Guid}")]
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
    [HttpPatch("{checklistGuid:Guid}/items/{itemGuid:Guid}")]
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
    [HttpPost("{checklistGuid:Guid}/sign")]
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
    [HttpGet("{checklistGuid:Guid}/scan")]
    [Authorize(Roles = nameof(Permissions.RentalRead))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> ScanDeviceOnChecklist(
        [FromRoute] Guid rentalGuid,
        [FromRoute] Guid checklistGuid,
        [FromQuery(Name = "device_guid")] Guid deviceGuid,
        CancellationToken ct)
    {
        var item = await checklistService.ScanDeviceOnChecklist(rentalGuid, checklistGuid, deviceGuid, ct);
        return Ok(item);
    }
}
