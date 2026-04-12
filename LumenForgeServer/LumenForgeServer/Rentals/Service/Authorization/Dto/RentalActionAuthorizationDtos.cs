using System.Security.Claims;
using LumenForgeServer.Rentals.Service.Actions;

namespace LumenForgeServer.Rentals.Service.Authorization.Dto;

/// <summary>
/// High-level decision outcome produced by rental action authorization checks.
/// </summary>
public enum RentalActionAuthorizationStatus
{
    Allowed,
    Forbidden,
    NotFound
}

/// <summary>
/// Detailed decision reason returned by rental action authorization checks.
/// </summary>
public enum RentalActionAuthorizationReason
{
    None,
    MissingReadScope,
    MissingUpdateScope,
    MissingActionPermission,
    MissingCreateScope,
    OutOfScope,
    StageNotAllowed,
    ProcessNotFound,
    ActorOwnsRental
}

/// <summary>
/// Input DTO for calculating available rental actions for a process.
/// </summary>
public sealed record GetAvailableRentalActionsRequestDto
{
    /// <summary>
    /// Authenticated caller principal used for scope and permission evaluation.
    /// </summary>
    public required ClaimsPrincipal User { get; init; }

    /// <summary>
    /// Target rental process identifier.
    /// </summary>
    public required Guid ProcessGuid { get; init; }
}

/// <summary>
/// Result DTO for available-action authorization checks.
/// </summary>
public sealed record GetAvailableRentalActionsResultDto
{
    /// <summary>
    /// Authorization status for the request.
    /// </summary>
    public required RentalActionAuthorizationStatus Status { get; init; }

    /// <summary>
    /// Optional reason providing context for non-allowed statuses.
    /// </summary>
    public RentalActionAuthorizationReason Reason { get; init; } = RentalActionAuthorizationReason.None;

    /// <summary>
    /// Filtered set of action types the caller may execute in the current stage.
    /// </summary>
    public IReadOnlyList<RentalActionType> Actions { get; init; } = [];
}

/// <summary>
/// Input DTO for authorizing execution of a concrete rental action.
/// </summary>
public sealed record AuthorizeRentalActionRequestDto
{
    /// <summary>
    /// Authenticated caller principal used for scope and permission evaluation.
    /// </summary>
    public required ClaimsPrincipal User { get; init; }

    /// <summary>
    /// Target rental process identifier.
    /// </summary>
    public required Guid ProcessGuid { get; init; }

    /// <summary>
    /// Action type the caller wants to execute.
    /// </summary>
    public required RentalActionType ActionType { get; init; }
}

/// <summary>
/// Result DTO for concrete action execution authorization.
/// </summary>
public sealed record AuthorizeRentalActionResultDto
{
    /// <summary>
    /// Authorization status for the request.
    /// </summary>
    public required RentalActionAuthorizationStatus Status { get; init; }

    /// <summary>
    /// Optional reason providing context for non-allowed statuses.
    /// </summary>
    public RentalActionAuthorizationReason Reason { get; init; } = RentalActionAuthorizationReason.None;
}

/// <summary>
/// Input DTO for authorizing rental creation before process instantiation.
/// </summary>
public sealed record AuthorizeCreateRentalActionRequestDto
{
    /// <summary>
    /// Authenticated caller principal used for scope and permission evaluation.
    /// </summary>
    public required ClaimsPrincipal User { get; init; }

    /// <summary>
    /// Optional owning group identifier specified for the new rental.
    /// </summary>
    public Guid? GroupGuid { get; init; }
}

/// <summary>
/// Result DTO for rental-create authorization checks.
/// </summary>
public sealed record AuthorizeCreateRentalActionResultDto
{
    /// <summary>
    /// Authorization status for the request.
    /// </summary>
    public required RentalActionAuthorizationStatus Status { get; init; }

    /// <summary>
    /// Optional reason providing context for non-allowed statuses.
    /// </summary>
    public RentalActionAuthorizationReason Reason { get; init; } = RentalActionAuthorizationReason.None;
}
