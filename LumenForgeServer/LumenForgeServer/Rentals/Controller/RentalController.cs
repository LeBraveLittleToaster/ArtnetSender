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
/// HTTP API for rental lifecycle management, stock-binding conflict checks, and status transitions.
/// </summary>
/// <remarks>
/// Routes are under <c>api/v1/rentals</c>.
/// </remarks>
[Route("api/v1/rentals")]
[ApiController]
[Authorize]
public class RentalController(RentalService rentalService) : ControllerBase
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
