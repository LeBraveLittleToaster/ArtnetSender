using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Persistence;
using LumenForgeServer.Rentals.Service.Actions;
using LumenForgeServer.Rentals.Service.Authorization.Dto;
using LumenForgeServer.Auth.Domain;

namespace LumenForgeServer.Rentals.Service.Authorization;

/// <summary>
/// Single source of truth for rental action authorization.
/// Core concept: the same decision pipeline is used for action discovery (<c>available</c>) and action execution
/// to keep UI-visible actions and backend enforcement consistent.
/// </summary>
/// <remarks>
/// Decision flow is intentionally ordered:
/// 1) scope gate (read or update depending on operation),
/// 2) process existence + in-scope check,
/// 3) stage gate,
/// 4) action permission mapping,
/// 5) ownership safety rules for selected approval actions.
/// This service is read-only and does not mutate rental workflow state.
/// </remarks>
public sealed class RentalActionAuthorizationService(
    IRentalProcessRepository processRepository,
    IRentalActionRegistry actionRegistry,
    RentalAccessService rentalAccessService) : IRentalActionAuthorizationService
{
    /// <summary>
    /// Returns all actions that are currently visible and executable for the caller on a process.
    /// Core concept: applies read-scope visibility first, then update/action gates to produce
    /// a UI-safe list that matches backend execution rules.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="request">Authorization context containing the caller and target rental process identifier.</param>
    /// <param name="ct">Cancellation token for repository and scope-resolution calls.</param>
    /// <returns>
    /// A status/result DTO containing either a forbid/not-found decision or the filtered list of allowed action types.
    /// </returns>
    public async Task<GetAvailableRentalActionsResultDto> GetAvailableActionsAsync(
        GetAvailableRentalActionsRequestDto request,
        CancellationToken ct)
    {
        var readScope = await rentalAccessService.BuildReadScopeAsync(request.User, ct);
        if (!readScope.HasAnyScope)
        {
            return new GetAvailableRentalActionsResultDto
            {
                Status = RentalActionAuthorizationStatus.Forbidden,
                Reason = RentalActionAuthorizationReason.MissingReadScope
            };
        }

        var process = await processRepository.GetByGuidAsync(request.ProcessGuid, ct);
        if (process is null)
        {
            return new GetAvailableRentalActionsResultDto
            {
                Status = RentalActionAuthorizationStatus.NotFound,
                Reason = RentalActionAuthorizationReason.ProcessNotFound
            };
        }

        if (!rentalAccessService.IsProcessInScope(process, readScope))
        {
            return new GetAvailableRentalActionsResultDto
            {
                Status = RentalActionAuthorizationStatus.Forbidden,
                Reason = RentalActionAuthorizationReason.OutOfScope
            };
        }

        var updateScope = await rentalAccessService.BuildUpdateScopeAsync(request.User, ct);
        if (!updateScope.HasAnyScope || !rentalAccessService.IsProcessInScope(process, updateScope))
        {
            return new GetAvailableRentalActionsResultDto
            {
                Status = RentalActionAuthorizationStatus.Allowed,
                Actions = []
            };
        }

        var hasUpdateAll = HasUpdateAllPermission(request.User);
        var actorKcId = RentalAccessService.ResolveCallerKcId(request.User);
        var actions = actionRegistry.GetAvailableActions(process.CurrentStage)
            .Where(actionType => hasUpdateAll || HasActionPermission(request.User, actionType))
            .Where(actionType => hasUpdateAll || IsActorOwnershipAllowed(actionType, actorKcId, process))
            .OrderBy(actionType => actionType)
            .ToList();

        return new GetAvailableRentalActionsResultDto
        {
            Status = RentalActionAuthorizationStatus.Allowed,
            Actions = actions
        };
    }

    /// <summary>
    /// Authorizes execution of a specific action against a rental process.
    /// Core concept: enforces the same stage/scope/permission/ownership gates that are used to calculate
    /// available actions so backend execution cannot diverge from UI visibility.
    /// </summary>
    /// <remarks>Potential side effects: read-only authorization check; no workflow mutation is performed here.</remarks>
    /// <param name="request">Authorization context containing caller, process identifier, and requested action type.</param>
    /// <param name="ct">Cancellation token for repository and scope-resolution calls.</param>
    /// <returns>A status/reason DTO indicating whether the action execution is allowed.</returns>
    public async Task<AuthorizeRentalActionResultDto> AuthorizeActionAsync(
        AuthorizeRentalActionRequestDto request,
        CancellationToken ct)
    {
        var updateScope = await rentalAccessService.BuildUpdateScopeAsync(request.User, ct);
        if (!updateScope.HasAnyScope)
        {
            return new AuthorizeRentalActionResultDto
            {
                Status = RentalActionAuthorizationStatus.Forbidden,
                Reason = RentalActionAuthorizationReason.MissingUpdateScope
            };
        }

        var process = await processRepository.GetByGuidAsync(request.ProcessGuid, ct);
        if (process is null)
        {
            return new AuthorizeRentalActionResultDto
            {
                Status = RentalActionAuthorizationStatus.NotFound,
                Reason = RentalActionAuthorizationReason.ProcessNotFound
            };
        }

        if (!rentalAccessService.IsProcessInScope(process, updateScope))
        {
            return new AuthorizeRentalActionResultDto
            {
                Status = RentalActionAuthorizationStatus.Forbidden,
                Reason = RentalActionAuthorizationReason.OutOfScope
            };
        }

        if (!actionRegistry.GetAvailableActions(process.CurrentStage).Contains(request.ActionType))
        {
            return new AuthorizeRentalActionResultDto
            {
                Status = RentalActionAuthorizationStatus.Forbidden,
                Reason = RentalActionAuthorizationReason.StageNotAllowed
            };
        }

        var hasUpdateAll = HasUpdateAllPermission(request.User);
        if (!hasUpdateAll && !HasActionPermission(request.User, request.ActionType))
        {
            return new AuthorizeRentalActionResultDto
            {
                Status = RentalActionAuthorizationStatus.Forbidden,
                Reason = RentalActionAuthorizationReason.MissingActionPermission
            };
        }

        var actorKcId = RentalAccessService.ResolveCallerKcId(request.User);
        if (!hasUpdateAll && !IsActorOwnershipAllowed(request.ActionType, actorKcId, process))
        {
            return new AuthorizeRentalActionResultDto
            {
                Status = RentalActionAuthorizationStatus.Forbidden,
                Reason = RentalActionAuthorizationReason.ActorOwnsRental
            };
        }

        return new AuthorizeRentalActionResultDto
        {
            Status = RentalActionAuthorizationStatus.Allowed
        };
    }

    /// <summary>
    /// Authorizes rental creation before the workflow process is created.
    /// Core concept: requires the explicit create-action permission (or update-all override) and validates
    /// ownership target scope (own vs group) through <see cref="RentalAccessService"/>.
    /// </summary>
    /// <remarks>Potential side effects: read-only authorization check; no persistence is performed by this method.</remarks>
    /// <param name="request">Authorization context containing caller identity and optional owning group identifier.</param>
    /// <param name="ct">Cancellation token for scope-resolution calls.</param>
    /// <returns>A status/reason DTO indicating whether create is allowed for the requested ownership target.</returns>
    public async Task<AuthorizeCreateRentalActionResultDto> AuthorizeCreateActionAsync(
        AuthorizeCreateRentalActionRequestDto request,
        CancellationToken ct)
    {
        if (!HasUpdateAllPermission(request.User) && !HasActionPermission(request.User, RentalActionType.CreateRental))
        {
            return new AuthorizeCreateRentalActionResultDto
            {
                Status = RentalActionAuthorizationStatus.Forbidden,
                Reason = RentalActionAuthorizationReason.MissingActionPermission
            };
        }

        if (!HasUpdateAllPermission(request.User) &&
            !await rentalAccessService.CanCreateRentalAsync(request.User, request.GroupGuid, ct))
        {
            return new AuthorizeCreateRentalActionResultDto
            {
                Status = RentalActionAuthorizationStatus.Forbidden,
                Reason = RentalActionAuthorizationReason.MissingCreateScope
            };
        }

        return new AuthorizeCreateRentalActionResultDto
        {
            Status = RentalActionAuthorizationStatus.Allowed
        };
    }

    /// <summary>
    /// Checks whether the caller has the explicit permission mapped to an action type.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="user">Authenticated principal whose role claims are evaluated.</param>
    /// <param name="actionType">Action type whose permission mapping is evaluated.</param>
    /// <returns><see langword="true"/> when the mapped action permission role is present; otherwise <see langword="false"/>.</returns>
    private static bool HasActionPermission(System.Security.Claims.ClaimsPrincipal user, RentalActionType actionType)
    {
        var policy = RentalActionPermissionPolicies.Get(actionType);
        return user.IsInRole(policy.Permission.ToString());
    }

    /// <summary>
    /// Checks whether the caller has the global rental update permission override.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="user">Authenticated principal whose role claims are evaluated.</param>
    /// <returns>
    /// <see langword="true"/> when <c>RentalUpdateAll</c> is assigned. This bypasses per-action permission and
    /// self-ownership restrictions, but stage gates still apply.
    /// </returns>
    private static bool HasUpdateAllPermission(System.Security.Claims.ClaimsPrincipal user)
        => user.IsInRole(nameof(Permissions.RentalUpdateAll));

    /// <summary>
    /// Evaluates ownership safety rules for actions that disallow self-approval.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="actionType">Action type being authorized.</param>
    /// <param name="actorKcId">Caller Keycloak subject identifier resolved from claims.</param>
    /// <param name="process">Process instance that contains rental ownership metadata.</param>
    /// <returns>
    /// <see langword="true"/> when self-ownership is allowed by policy or the caller is not the rental owner;
    /// otherwise <see langword="false"/>.
    /// </returns>
    private static bool IsActorOwnershipAllowed(
        RentalActionType actionType,
        string? actorKcId,
        RentalProcessInstance process)
    {
        var policy = RentalActionPermissionPolicies.Get(actionType);
        if (!policy.ForbidIfActorOwnsRental)
            return true;

        if (string.IsNullOrWhiteSpace(actorKcId))
            return false;

        var ownerKcId = process.Rental?.CustomerKcId;
        return !string.Equals(ownerKcId, actorKcId, StringComparison.Ordinal);
    }
}
