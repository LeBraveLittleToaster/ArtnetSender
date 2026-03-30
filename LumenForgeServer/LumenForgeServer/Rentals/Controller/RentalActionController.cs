using System.Security.Claims;
using LumenForgeServer.Rentals.Dto.Command;
using LumenForgeServer.Rentals.Dto.View;
using LumenForgeServer.Rentals.Persistence;
using LumenForgeServer.Rentals.Service;
using LumenForgeServer.Rentals.Service.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LumenForgeServer.Rentals.Controller;

/// <summary>
/// HTTP API for executing rental process actions.
/// Each action has its own endpoint for easy discoverability and extension.
/// All endpoints delegate to the <see cref="RentalActionService"/> orchestrator.
/// </summary>
/// <remarks>
/// <para>Actions are stage-gated: an action is only available when the process is
/// in a stage that permits it. Call <c>GET {processGuid}/available</c> to discover
/// which actions are currently allowed.</para>
/// <para>The actor identity is always taken from the JWT <c>sub</c> claim and
/// cannot be overridden through the request body.</para>
/// </remarks>
[Route("api/v1/rentals/actions")]
[ApiController]
[Authorize]
[Tags("Rentals – Actions")]
public class RentalActionController(
    RentalActionService actionService,
    RentalAccessService rentalAccessService,
    IRentalProcessRepository processRepository) : ControllerBase
{
    // ── Process queries ─────────────────────────────────────────────

    /// <summary>Returns the actions available for the given process instance based on its current stage and the caller’s permissions.</summary>
    /// <param name="processGuid">Process instance identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 200 response with the list of allowed action types.</returns>
    [HttpGet("{processGuid:guid}/available")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize]
    public async Task<IActionResult> GetAvailableActions(
        [FromRoute] Guid processGuid, CancellationToken ct)
    {
        var readScope = await rentalAccessService.BuildReadScopeAsync(User, ct);
        if (!readScope.HasAnyScope)
            return Forbid();

        var process = await processRepository.GetByGuidAsync(processGuid, ct);
        if (process is null)
            return NotFound();

        if (!rentalAccessService.IsProcessInScope(process, readScope))
            return Forbid();

        var updateScope = await rentalAccessService.BuildUpdateScopeAsync(User, ct);
        if (!updateScope.HasAnyScope || !rentalAccessService.IsProcessInScope(process, updateScope))
            return Ok(Array.Empty<RentalActionType>());

        var actions = await actionService.GetAvailableActionsAsync(processGuid, ct);
        return Ok(actions);
    }

    // ── Create ──────────────────────────────────────────────────────

    /// <summary>Creates a new rental and starts its process instance.</summary>
    /// <remarks>Side-effects: creates a <c>RentalProcessInstance</c> in the <c>Requested</c> stage, a <c>Rental</c> entity, and an action log entry.</remarks>
    /// <param name="dto">Rental creation payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 201 response with the action result.</returns>
    [HttpPost("create")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Authorize]
    public async Task<IActionResult> CreateRental(
        [FromBody] CreateRentalDto dto, CancellationToken ct)
    {
        if (!await rentalAccessService.CanCreateRentalAsync(User, dto.GroupGuid, ct))
            return Forbid();

        var input = dto.ToActionInput();
        SetActor(input);
        var result = await actionService.CreateProcessAsync(input, ct);
        return StatusCode(StatusCodes.Status201Created, ActionResultView.FromActionResult(result));
    }

    // ── Request approval ────────────────────────────────────────────

    /// <summary>Approves a rental request, advancing the process to the Approved stage.</summary>
    /// <param name="processGuid">Process instance identifier.</param>
    /// <param name="dto">Optional approval comment.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("{processGuid:guid}/approve-request")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize]
    public async Task<IActionResult> ApproveRequest(
        [FromRoute] Guid processGuid, [FromBody] ApproveRequestDto dto, CancellationToken ct)
        => await ExecuteAsync(processGuid, RentalActionType.ApproveRequest, dto.ToActionInput(), ct);

    /// <summary>Rejects a rental request and moves the process to the Cancelled stage.</summary>
    /// <param name="processGuid">Process instance identifier.</param>
    /// <param name="dto">Rejection reason (required).</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("{processGuid:guid}/reject-request")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize]
    public async Task<IActionResult> RejectRequest(
        [FromRoute] Guid processGuid, [FromBody] RejectRequestDto dto, CancellationToken ct)
        => await ExecuteAsync(processGuid, RentalActionType.RejectRequest, dto.ToActionInput(), ct);

    // ── Item management ─────────────────────────────────────────────

    /// <summary>Assigns inventory items (devices + quantities) to the rental. Creates stock bindings.</summary>
    /// <param name="processGuid">Process instance identifier.</param>
    /// <param name="dto">List of device GUIDs and quantities.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("{processGuid:guid}/assign-items")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize]
    public async Task<IActionResult> AssignItems(
        [FromRoute] Guid processGuid, [FromBody] AssignItemsDto dto, CancellationToken ct)
        => await ExecuteAsync(processGuid, RentalActionType.AssignItems, dto.ToActionInput(), ct);

    /// <summary>Removes previously assigned items from the rental by their stock-binding GUIDs.</summary>
    /// <param name="processGuid">Process instance identifier.</param>
    /// <param name="dto">Stock binding GUIDs to remove.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("{processGuid:guid}/remove-items")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize]
    public async Task<IActionResult> RemoveItems(
        [FromRoute] Guid processGuid, [FromBody] RemoveItemsDto dto, CancellationToken ct)
        => await ExecuteAsync(processGuid, RentalActionType.RemoveItems, dto.ToActionInput(), ct);

    /// <summary>Approves the currently assigned item list, locking in the devices for pickup.</summary>
    /// <param name="processGuid">Process instance identifier.</param>
    /// <param name="dto">Optional approval comment.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("{processGuid:guid}/approve-items")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize]
    public async Task<IActionResult> ApproveItems(
        [FromRoute] Guid processGuid, [FromBody] ApproveItemsDto dto, CancellationToken ct)
        => await ExecuteAsync(processGuid, RentalActionType.ApproveItems, dto.ToActionInput(), ct);

    /// <summary>Rejects the currently assigned item list, requiring re-assignment.</summary>
    /// <param name="processGuid">Process instance identifier.</param>
    /// <param name="dto">Rejection reason (required).</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("{processGuid:guid}/reject-items")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize]
    public async Task<IActionResult> RejectItems(
        [FromRoute] Guid processGuid, [FromBody] RejectItemsDto dto, CancellationToken ct)
        => await ExecuteAsync(processGuid, RentalActionType.RejectItems, dto.ToActionInput(), ct);

    // ── Checklists ──────────────────────────────────────────────────

    /// <summary>Generates a checklist (pickup or dropoff) from the rental’s assigned items.</summary>
    /// <param name="processGuid">Process instance identifier.</param>
    /// <param name="dto">Checklist type to generate.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("{processGuid:guid}/generate-checklist")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize]
    public async Task<IActionResult> GenerateChecklist(
        [FromRoute] Guid processGuid, [FromBody] GenerateChecklistDto dto, CancellationToken ct)
        => await ExecuteAsync(processGuid, RentalActionType.GenerateChecklist, dto.ToActionInput(), ct);

    /// <summary>Records a QR / barcode scan against a checklist item. Marks the item as scanned.</summary>
    /// <param name="processGuid">Process instance identifier.</param>
    /// <param name="dto">Checklist GUID and scanned value.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("{processGuid:guid}/scan-checklist")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize]
    public async Task<IActionResult> ScanChecklist(
        [FromRoute] Guid processGuid, [FromBody] ScanChecklistDto dto, CancellationToken ct)
        => await ExecuteAsync(processGuid, RentalActionType.ScanChecklist, dto.ToActionInput(), ct);

    /// <summary>Records a digital signature on a checklist, finalising it.</summary>
    /// <param name="processGuid">Process instance identifier.</param>
    /// <param name="dto">Checklist GUID and base-64 signature data.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("{processGuid:guid}/sign-checklist")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize]
    public async Task<IActionResult> SignChecklist(
        [FromRoute] Guid processGuid, [FromBody] SignChecklistDto dto, CancellationToken ct)
        => await ExecuteAsync(processGuid, RentalActionType.SignChecklist, dto.ToActionInput(), ct);

    // ── Pickup / Return ─────────────────────────────────────────────

    /// <summary>Records that items were picked up by the customer. Advances the process to the Active stage.</summary>
    /// <param name="processGuid">Process instance identifier.</param>
    /// <param name="dto">Optional pickup notes.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("{processGuid:guid}/record-pickup")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize]
    public async Task<IActionResult> RecordPickup(
        [FromRoute] Guid processGuid, [FromBody] RecordPickupDto dto, CancellationToken ct)
        => await ExecuteAsync(processGuid, RentalActionType.RecordPickup, dto.ToActionInput(), ct);

    /// <summary>Records that items were returned by the customer. Advances the process to the Returned stage.</summary>
    /// <param name="processGuid">Process instance identifier.</param>
    /// <param name="dto">Optional return notes.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("{processGuid:guid}/record-return")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize]
    public async Task<IActionResult> RecordReturn(
        [FromRoute] Guid processGuid, [FromBody] RecordReturnDto dto, CancellationToken ct)
        => await ExecuteAsync(processGuid, RentalActionType.RecordReturn, dto.ToActionInput(), ct);

    // ── Extensions ──────────────────────────────────────────────────

    /// <summary>Submits a request to extend the rental period to a new end date.</summary>
    /// <param name="processGuid">Process instance identifier.</param>
    /// <param name="dto">New end date and optional reason.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("{processGuid:guid}/request-extension")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize]
    public async Task<IActionResult> RequestExtension(
        [FromRoute] Guid processGuid, [FromBody] RequestExtensionDto dto, CancellationToken ct)
        => await ExecuteAsync(processGuid, RentalActionType.RequestExtension, dto.ToActionInput(), ct);

    /// <summary>Approves a pending extension request. Updates the rental’s requested end date.</summary>
    /// <param name="processGuid">Process instance identifier.</param>
    /// <param name="dto">Extension GUID and optional comment.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("{processGuid:guid}/approve-extension")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize]
    public async Task<IActionResult> ApproveExtension(
        [FromRoute] Guid processGuid, [FromBody] ApproveExtensionDto dto, CancellationToken ct)
        => await ExecuteAsync(processGuid, RentalActionType.ApproveExtension, dto.ToActionInput(), ct);

    /// <summary>Rejects a pending extension request with a required reason.</summary>
    /// <param name="processGuid">Process instance identifier.</param>
    /// <param name="dto">Extension GUID and rejection reason.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("{processGuid:guid}/reject-extension")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize]
    public async Task<IActionResult> RejectExtension(
        [FromRoute] Guid processGuid, [FromBody] RejectExtensionDto dto, CancellationToken ct)
        => await ExecuteAsync(processGuid, RentalActionType.RejectExtension, dto.ToActionInput(), ct);

    // ── Post-return ─────────────────────────────────────────────────

    /// <summary>Records one or more damage reports found during post-return inspection.</summary>
    /// <param name="processGuid">Process instance identifier.</param>
    /// <param name="dto">List of damage entries (device, description, severity).</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("{processGuid:guid}/record-damages")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize]
    public async Task<IActionResult> RecordDamages(
        [FromRoute] Guid processGuid, [FromBody] RecordDamagesDto dto, CancellationToken ct)
        => await ExecuteAsync(processGuid, RentalActionType.RecordDamages, dto.ToActionInput(), ct);

    /// <summary>Creates maintenance jobs for damaged stock bindings. Links them to the rental.</summary>
    /// <param name="processGuid">Process instance identifier.</param>
    /// <param name="dto">List of damaged stock-binding GUIDs.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("{processGuid:guid}/create-maintenance-jobs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize]
    public async Task<IActionResult> CreateMaintenanceJobs(
        [FromRoute] Guid processGuid, [FromBody] CreateMaintenanceJobsDto dto, CancellationToken ct)
        => await ExecuteAsync(processGuid, RentalActionType.CreateMaintenanceJobs, dto.ToActionInput(), ct);

    // ── Billing ─────────────────────────────────────────────────────

    /// <summary>Generates an invoice for the rental based on the rental period and assigned items.</summary>
    /// <param name="processGuid">Process instance identifier.</param>
    /// <param name="dto">Optional due-date override.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("{processGuid:guid}/generate-invoice")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize]
    public async Task<IActionResult> GenerateInvoice(
        [FromRoute] Guid processGuid, [FromBody] GenerateInvoiceDto dto, CancellationToken ct)
        => await ExecuteAsync(processGuid, RentalActionType.GenerateInvoice, dto.ToActionInput(), ct);

    /// <summary>Records a payment against an existing invoice. Supports CASH, CARD, TRANSFER, or OTHER.</summary>
    /// <param name="processGuid">Process instance identifier.</param>
    /// <param name="dto">Invoice GUID, amount, method, and optional reference.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("{processGuid:guid}/record-payment")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize]
    public async Task<IActionResult> RecordPayment(
        [FromRoute] Guid processGuid, [FromBody] RecordPaymentDto dto, CancellationToken ct)
        => await ExecuteAsync(processGuid, RentalActionType.RecordPayment, dto.ToActionInput(), ct);

    // ── Reporting ───────────────────────────────────────────────────

    /// <summary>Generates a summary report for the rental. Can optionally include damages and payments.</summary>
    /// <param name="processGuid">Process instance identifier.</param>
    /// <param name="dto">Flags for including damages and/or payments.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("{processGuid:guid}/generate-report")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize]
    public async Task<IActionResult> GenerateReport(
        [FromRoute] Guid processGuid, [FromBody] GenerateReportDto dto, CancellationToken ct)
        => await ExecuteAsync(processGuid, RentalActionType.GenerateReport, dto.ToActionInput(), ct);

    // ── Lifecycle ───────────────────────────────────────────────────

    /// <summary>Completes the rental, advancing the process to the terminal Completed stage.</summary>
    /// <param name="processGuid">Process instance identifier.</param>
    /// <param name="dto">Optional completion comment.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("{processGuid:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize]
    public async Task<IActionResult> CompleteRental(
        [FromRoute] Guid processGuid, [FromBody] CompleteRentalDto dto, CancellationToken ct)
        => await ExecuteAsync(processGuid, RentalActionType.CompleteRental, dto.ToActionInput(), ct);

    /// <summary>Cancels the rental, advancing the process to the terminal Cancelled stage.</summary>
    /// <param name="processGuid">Process instance identifier.</param>
    /// <param name="dto">Cancellation reason (required).</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("{processGuid:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize]
    public async Task<IActionResult> CancelRental(
        [FromRoute] Guid processGuid, [FromBody] CancelRentalDto dto, CancellationToken ct)
        => await ExecuteAsync(processGuid, RentalActionType.CancelRental, dto.ToActionInput(), ct);

    /// <summary>Scraps the rental (total write-off), advancing the process to the terminal Scrapped stage.</summary>
    /// <param name="processGuid">Process instance identifier.</param>
    /// <param name="dto">Scrap reason (required).</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost("{processGuid:guid}/scrap")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize]
    public async Task<IActionResult> ScrapRental(
        [FromRoute] Guid processGuid, [FromBody] ScrapRentalDto dto, CancellationToken ct)
        => await ExecuteAsync(processGuid, RentalActionType.ScrapRental, dto.ToActionInput(), ct);

    // ── Helpers ─────────────────────────────────────────────────────

    /// <summary>Shorthand that sets the actor from the token and delegates to the orchestrator.</summary>
    private async Task<IActionResult> ExecuteAsync(
        Guid processGuid, RentalActionType actionType, ActionInput input, CancellationToken ct)
    {
        var updateScope = await rentalAccessService.BuildUpdateScopeAsync(User, ct);
        if (!updateScope.HasAnyScope)
            return Forbid();

        var process = await processRepository.GetByGuidAsync(processGuid, ct);
        if (process is null)
            return NotFound();

        if (!rentalAccessService.IsProcessInScope(process, updateScope))
            return Forbid();

        SetActor(input);
        var result = await actionService.ExecuteActionAsync(processGuid, actionType, input, ct);
        return Ok(ActionResultView.FromActionResult(result));
    }

    /// <summary>
    /// Populates <see cref="ActionInput.ActorKcId"/> from the authenticated JWT token.
    /// Always overwrites — the value is never accepted from the request body.
    /// </summary>
    private void SetActor(ActionInput input)
    {
        input.ActorKcId = User.FindFirstValue("sub")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Missing subject claim in token.");
    }
}
