using System.Security.Claims;
using LumenForgeServer.Auth.Domain;
using LumenForgeServer.Rentals.Dto.Command;
using LumenForgeServer.Rentals.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LumenForgeServer.Rentals.Controller;

/// <summary>
/// HTTP API for the action-based rental workflow.
/// </summary>
/// <remarks>
/// Routes are under <c>api/v1/rentals/{rentalGuid}/actions</c>.
/// </remarks>
[Route("api/v1/rentals/{rentalGuid:guid}/actions")]
[ApiController]
[Authorize]
public class RentalActionController(RentalActionService actionService) : ControllerBase
{
    /// <summary>
    /// Returns the actions that can currently be executed on the rental.
    /// </summary>
    [HttpGet("available")]
    [Authorize(Roles = nameof(Permissions.RentalRead))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> GetAvailableActions(
        [FromRoute] Guid rentalGuid,
        CancellationToken ct)
    {
        var available = await actionService.GetAvailableActionsAsync(rentalGuid, ct);
        return Ok(new { list = available, total = available.Count });
    }

    /// <summary>
    /// Executes an action on the rental.
    /// </summary>
    [HttpPost("")]
    [Authorize(Roles = nameof(Permissions.RentalUpdate))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> ExecuteAction(
        [FromRoute] Guid rentalGuid,
        [FromBody] ExecuteActionDto dto,
        CancellationToken ct)
    {
        var actorUserId = User.FindFirstValue("sub")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.Identity?.Name
            ?? "unknown-user";

        var result = await actionService.ExecuteActionAsync(
            rentalGuid, dto.ActionType, dto.Input, actorUserId, ct);

        return Ok(result);
    }

    /// <summary>
    /// Lists the executed action history for a rental (newest first).
    /// </summary>
    [HttpGet("")]
    [Authorize(Roles = nameof(Permissions.RentalRead))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> ListActions(
        [FromRoute] Guid rentalGuid,
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0,
        CancellationToken ct = default)
    {
        var (items, total) = await actionService.ListActionsAsync(rentalGuid, limit, offset, ct);
        return Ok(new { list = items, total });
    }
}
