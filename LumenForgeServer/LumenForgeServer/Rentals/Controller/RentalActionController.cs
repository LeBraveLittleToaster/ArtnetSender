using System.Security.Claims;
using LumenForgeServer.Auth.Domain;
using LumenForgeServer.Rentals.Service;
using LumenForgeServer.Rentals.Service.Actions;
using LumenForgeServer.Rentals.Service.Actions.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ActionResult = LumenForgeServer.Rentals.Service.Actions.ActionResult;

namespace LumenForgeServer.Rentals.Controller;

/// <summary>
/// HTTP API for executing rental process actions.
/// Each action has its own endpoint for easy discoverability and extension.
/// All endpoints delegate to the <see cref="RentalActionService"/> orchestrator.
/// </summary>
[Route("api/v1/rentals/actions")]
[ApiController]
[Authorize]
public class RentalActionController(RentalActionService actionService) : ControllerBase
{
    // ── Process queries ─────────────────────────────────────────────

    /// <summary>Returns the actions available for the given process instance.</summary>
    [HttpGet("{processGuid:guid}/available")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAvailableActions(
        [FromRoute] Guid processGuid, CancellationToken ct)
    {
        var permissions = User.GetAppPermissions();
        var actions = await actionService.GetAvailableActionsAsync(processGuid, permissions, ct);
        return Ok(actions);
    }

    // ── Create ──────────────────────────────────────────────────────

    /// <summary>Creates a new rental and starts its process instance.</summary>
    [HttpPost("create")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRental(
        [FromBody] CreateRentalInput input, CancellationToken ct)
    {
        SetActor(input);
        var permissions =  User.GetAppPermissions();
        var result = await actionService.CreateProcessAsync(input, permissions, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    // ── Request approval ────────────────────────────────────────────

    /// <summary>Approves a rental request.</summary>
    [HttpPost("{processGuid:guid}/approve-request")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ApproveRequest(
        [FromRoute] Guid processGuid, [FromBody] ApproveRequestInput input, CancellationToken ct)
        => Ok(await ExecuteAsync(processGuid, RentalActionType.ApproveRequest, input, User.GetAppPermissions(),ct));

    /// <summary>Rejects a rental request.</summary>
    [HttpPost("{processGuid:guid}/reject-request")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RejectRequest(
        [FromRoute] Guid processGuid, [FromBody] RejectRequestInput input, CancellationToken ct)
        => Ok(await ExecuteAsync(processGuid, RentalActionType.RejectRequest, input, User.GetAppPermissions(),ct));

    // ── Item management ─────────────────────────────────────────────

    /// <summary>Assigns inventory items to the rental.</summary>
    [HttpPost("{processGuid:guid}/assign-items")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> AssignItems(
        [FromRoute] Guid processGuid, [FromBody] AssignItemsInput input, CancellationToken ct)
        => Ok(await ExecuteAsync(processGuid, RentalActionType.AssignItems, input, User.GetAppPermissions(),ct));

    /// <summary>Removes assigned items from the rental.</summary>
    [HttpPost("{processGuid:guid}/remove-items")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveItems(
        [FromRoute] Guid processGuid, [FromBody] RemoveItemsInput input, CancellationToken ct)
        => Ok(await ExecuteAsync(processGuid, RentalActionType.RemoveItems, input, User.GetAppPermissions(),ct));

    /// <summary>Approves the assigned item list.</summary>
    [HttpPost("{processGuid:guid}/approve-items")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ApproveItems(
        [FromRoute] Guid processGuid, [FromBody] ApproveItemsInput input, CancellationToken ct)
        => Ok(await ExecuteAsync(processGuid, RentalActionType.ApproveItems, input, User.GetAppPermissions(),ct));

    /// <summary>Rejects the assigned item list.</summary>
    [HttpPost("{processGuid:guid}/reject-items")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RejectItems(
        [FromRoute] Guid processGuid, [FromBody] RejectItemsInput input, CancellationToken ct)
        => Ok(await ExecuteAsync(processGuid, RentalActionType.RejectItems, input, User.GetAppPermissions(),ct));

    // ── Checklists ──────────────────────────────────────────────────

    /// <summary>Generates a checklist for the rental.</summary>
    [HttpPost("{processGuid:guid}/generate-checklist")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateChecklist(
        [FromRoute] Guid processGuid, [FromBody] GenerateChecklistInput input, CancellationToken ct)
        => Ok(await ExecuteAsync(processGuid, RentalActionType.GenerateChecklist, input, User.GetAppPermissions(),ct));

    /// <summary>Records a device scan against a checklist.</summary>
    [HttpPost("{processGuid:guid}/scan-checklist")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ScanChecklist(
        [FromRoute] Guid processGuid, [FromBody] ScanChecklistInput input, CancellationToken ct)
        => Ok(await ExecuteAsync(processGuid, RentalActionType.ScanChecklist, input, User.GetAppPermissions(),ct));

    /// <summary>Records a signature on a checklist.</summary>
    [HttpPost("{processGuid:guid}/sign-checklist")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SignChecklist(
        [FromRoute] Guid processGuid, [FromBody] SignChecklistInput input, CancellationToken ct)
        => Ok(await ExecuteAsync(processGuid, RentalActionType.SignChecklist, input, User.GetAppPermissions(),ct));

    // ── Pickup / Return ─────────────────────────────────────────────

    /// <summary>Records that items were picked up.</summary>
    [HttpPost("{processGuid:guid}/record-pickup")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RecordPickup(
        [FromRoute] Guid processGuid, [FromBody] RecordPickupInput input, CancellationToken ct)
        => Ok(await ExecuteAsync(processGuid, RentalActionType.RecordPickup, input, User.GetAppPermissions(),ct));

    /// <summary>Records that items were returned.</summary>
    [HttpPost("{processGuid:guid}/record-return")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RecordReturn(
        [FromRoute] Guid processGuid, [FromBody] RecordReturnInput input, CancellationToken ct)
        => Ok(await ExecuteAsync(processGuid, RentalActionType.RecordReturn, input, User.GetAppPermissions(),ct));

    // ── Extensions ──────────────────────────────────────────────────

    /// <summary>Submits an extension request.</summary>
    [HttpPost("{processGuid:guid}/request-extension")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RequestExtension(
        [FromRoute] Guid processGuid, [FromBody] RequestExtensionInput input, CancellationToken ct)
        => Ok(await ExecuteAsync(processGuid, RentalActionType.RequestExtension, input, User.GetAppPermissions(),ct));

    /// <summary>Approves an extension request.</summary>
    [HttpPost("{processGuid:guid}/approve-extension")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ApproveExtension(
        [FromRoute] Guid processGuid, [FromBody] ApproveExtensionInput input, CancellationToken ct)
        => Ok(await ExecuteAsync(processGuid, RentalActionType.ApproveExtension, input, User.GetAppPermissions(),ct));

    /// <summary>Rejects an extension request.</summary>
    [HttpPost("{processGuid:guid}/reject-extension")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RejectExtension(
        [FromRoute] Guid processGuid, [FromBody] RejectExtensionInput input, CancellationToken ct)
        => Ok(await ExecuteAsync(processGuid, RentalActionType.RejectExtension, input, User.GetAppPermissions(),ct));

    // ── Post-return ─────────────────────────────────────────────────

    /// <summary>Records damages found during inspection.</summary>
    [HttpPost("{processGuid:guid}/record-damages")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RecordDamages(
        [FromRoute] Guid processGuid, [FromBody] RecordDamagesInput input, CancellationToken ct)
        => Ok(await ExecuteAsync(processGuid, RentalActionType.RecordDamages, input, User.GetAppPermissions(),ct));

    /// <summary>Creates maintenance jobs for damaged items.</summary>
    [HttpPost("{processGuid:guid}/create-maintenance-jobs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateMaintenanceJobs(
        [FromRoute] Guid processGuid, [FromBody] CreateMaintenanceJobsInput input, CancellationToken ct)
        => Ok(await ExecuteAsync(processGuid, RentalActionType.CreateMaintenanceJobs, input, User.GetAppPermissions(),ct));

    // ── Billing ─────────────────────────────────────────────────────

    /// <summary>Generates an invoice for the rental.</summary>
    [HttpPost("{processGuid:guid}/generate-invoice")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateInvoice(
        [FromRoute] Guid processGuid, [FromBody] GenerateInvoiceInput input, CancellationToken ct)
        => Ok(await ExecuteAsync(processGuid, RentalActionType.GenerateInvoice, input, User.GetAppPermissions(),ct));

    /// <summary>Records a payment against the invoice.</summary>
    [HttpPost("{processGuid:guid}/record-payment")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RecordPayment(
        [FromRoute] Guid processGuid, [FromBody] RecordPaymentInput input, CancellationToken ct)
        => Ok(await ExecuteAsync(processGuid, RentalActionType.RecordPayment, input, User.GetAppPermissions(),ct));

    // ── Reporting ───────────────────────────────────────────────────

    /// <summary>Generates a summary report.</summary>
    [HttpPost("{processGuid:guid}/generate-report")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateReport(
        [FromRoute] Guid processGuid, [FromBody] GenerateReportInput input, CancellationToken ct)
        => Ok(await ExecuteAsync(processGuid, RentalActionType.GenerateReport, input, User.GetAppPermissions(),ct));

    // ── Lifecycle ───────────────────────────────────────────────────

    /// <summary>Completes the rental.</summary>
    [HttpPost("{processGuid:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CompleteRental(
        [FromRoute] Guid processGuid, [FromBody] CompleteRentalInput input, CancellationToken ct)
        => Ok(await ExecuteAsync(processGuid, RentalActionType.CompleteRental, input, User.GetAppPermissions(),ct));

    /// <summary>Cancels the rental.</summary>
    [HttpPost("{processGuid:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CancelRental(
        [FromRoute] Guid processGuid, [FromBody] CancelRentalInput input, CancellationToken ct)
        => Ok(await ExecuteAsync(processGuid, RentalActionType.CancelRental, input, User.GetAppPermissions(),ct));

    /// <summary>Scraps the rental (total write-off).</summary>
    [HttpPost("{processGuid:guid}/scrap")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ScrapRental(
        [FromRoute] Guid processGuid, [FromBody] ScrapRentalInput input, CancellationToken ct)
        => Ok(await ExecuteAsync(processGuid, RentalActionType.ScrapRental, input, User.GetAppPermissions(), ct));

    // ── Helpers ─────────────────────────────────────────────────────

    /// <summary>Shorthand that sets the actor from the token and delegates to the orchestrator.</summary>
    private async Task<ActionResult> ExecuteAsync(
        Guid processGuid, RentalActionType actionType, ActionInput input, IReadOnlyList<Permissions> permissions, CancellationToken ct)
    {
        SetActor(input);
        return await actionService.ExecuteActionAsync(processGuid, actionType, permissions, input, ct);
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
