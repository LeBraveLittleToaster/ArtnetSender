using System.Security.Claims;
using LumenForgeServer.Auth.Domain;
using LumenForgeServer.Auth.Persistence;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Dto.Query;

namespace LumenForgeServer.Rentals.Service;

/// <summary>
/// Centralizes rental scope resolution for read, update, and create authorization checks.
/// Core concept: global permissions (<c>RentalReadAll</c>/<c>RentalUpdateAll</c>) map to allow-all scope,
/// while scoped permissions (<c>RentalUserOwn</c>/<c>RentalGroup</c>) map to owner/group filters.
/// </summary>
/// <remarks>
/// This service is read-only from a domain-state perspective. It may query auth persistence for group memberships
/// when group-based scope checks are required.
/// </remarks>
public sealed class RentalAccessService(IAuthRepository authRepository)
{
    /// <summary>
    /// Builds the effective read scope for the current caller.
    /// Core concept: returns allow-all for <c>RentalReadAll</c>, otherwise resolves own/group filters from claims
    /// and persisted group memberships.
    /// </summary>
    /// <remarks>Potential side effects: may query group memberships from the auth repository.</remarks>
    /// <param name="user">Authenticated caller principal whose rental permissions and identity are evaluated.</param>
    /// <param name="ct">Cancellation token for repository calls during scope resolution.</param>
    /// <returns>A scope filter used to constrain rental read queries and process visibility checks.</returns>
    public async Task<RentalAccessFilter> BuildReadScopeAsync(ClaimsPrincipal user, CancellationToken ct)
        => await BuildScopeAsync(user, nameof(Permissions.RentalReadAll), ct);

    /// <summary>
    /// Builds the effective update scope for the current caller.
    /// Core concept: returns allow-all for <c>RentalUpdateAll</c>, otherwise resolves own/group filters from claims
    /// and persisted group memberships.
    /// </summary>
    /// <remarks>Potential side effects: may query group memberships from the auth repository.</remarks>
    /// <param name="user">Authenticated caller principal whose rental permissions and identity are evaluated.</param>
    /// <param name="ct">Cancellation token for repository calls during scope resolution.</param>
    /// <returns>A scope filter used to constrain rental update checks and action authorization.</returns>
    public async Task<RentalAccessFilter> BuildUpdateScopeAsync(ClaimsPrincipal user, CancellationToken ct)
        => await BuildScopeAsync(user, nameof(Permissions.RentalUpdateAll), ct);

    /// <summary>
    /// Determines whether the caller may create a rental for the requested ownership target.
    /// Core concept: <c>RentalCreate</c> grants unrestricted create, <c>RentalUserOwn</c> grants own create
    /// (no group target), and <c>RentalGroup</c> grants create for groups the caller belongs to.
    /// </summary>
    /// <remarks>Potential side effects: may query group memberships from the auth repository.</remarks>
    /// <param name="user">Authenticated caller principal whose permissions and identity are evaluated.</param>
    /// <param name="groupGuid">Optional owning group identifier for the rental creation request.</param>
    /// <param name="ct">Cancellation token for repository calls during group-membership lookup.</param>
    /// <returns><see langword="true"/> when creation is allowed for the given ownership target; otherwise <see langword="false"/>.</returns>
    public async Task<bool> CanCreateRentalAsync(ClaimsPrincipal user, Guid? groupGuid, CancellationToken ct)
    {
        if (user.IsInRole(nameof(Permissions.RentalCreate)))
            return true;

        var callerKcId = ResolveCallerKcId(user);
        if (string.IsNullOrWhiteSpace(callerKcId))
            return false;

        if (groupGuid is null)
            return user.IsInRole(nameof(Permissions.RentalUserOwn));

        if (!user.IsInRole(nameof(Permissions.RentalGroup)))
            return false;

        var callerGroups = await authRepository.GetGroupGuidsForUserAsync(callerKcId, ct);
        return callerGroups.Contains(groupGuid.Value);
    }

    /// <summary>
    /// Checks whether a rental process instance is covered by a resolved access scope.
    /// Core concept: allow-all scope bypasses ownership checks; otherwise the process must match owner scope
    /// or one of the allowed group scopes.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="process">Rental process instance to evaluate, including its linked rental ownership metadata.</param>
    /// <param name="scope">Previously resolved caller access scope.</param>
    /// <returns><see langword="true"/> when the process is inside the caller scope; otherwise <see langword="false"/>.</returns>
    public bool IsProcessInScope(RentalProcessInstance process, RentalAccessFilter scope)
    {
        if (scope.AllowAll)
            return true;

        if (process.Rental is null)
            return false;

        var matchesOwner = !string.IsNullOrWhiteSpace(scope.OwnerKcId)
                           && string.Equals(process.Rental.CustomerKcId, scope.OwnerKcId, StringComparison.Ordinal);
        var matchesGroup = process.Rental.GroupGuid.HasValue && scope.GroupGuids.Contains(process.Rental.GroupGuid.Value);

        return matchesOwner || matchesGroup;
    }

    /// <summary>
    /// Resolves the caller Keycloak subject identifier from claims.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="user">Authenticated principal whose claim set is inspected.</param>
    /// <returns>
    /// The caller identifier from <c>sub</c> or <see cref="ClaimTypes.NameIdentifier"/> when present;
    /// otherwise <see langword="null"/>.
    /// </returns>
    public static string? ResolveCallerKcId(ClaimsPrincipal user)
        => user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <summary>
    /// Builds a caller scope for the supplied global permission role and scoped rental permissions.
    /// Core concept: evaluates global allow-all first, then composes own/group constraints.
    /// </summary>
    /// <remarks>Potential side effects: may query group memberships from the auth repository.</remarks>
    /// <param name="user">Authenticated caller principal used for role and identity evaluation.</param>
    /// <param name="globalPermissionRole">Role name that represents unrestricted scope for the requested operation.</param>
    /// <param name="ct">Cancellation token for repository calls during group-membership lookup.</param>
    /// <returns>A resolved access filter containing allow-all, owner, and/or group constraints.</returns>
    private async Task<RentalAccessFilter> BuildScopeAsync(
        ClaimsPrincipal user,
        string globalPermissionRole,
        CancellationToken ct)
    {
        if (user.IsInRole(globalPermissionRole))
            return RentalAccessFilter.AllowAllScope;

        var hasOwnScope = user.IsInRole(nameof(Permissions.RentalUserOwn));
        var hasGroupScope = user.IsInRole(nameof(Permissions.RentalGroup));
        if (!hasOwnScope && !hasGroupScope)
            return new RentalAccessFilter();

        var callerKcId = ResolveCallerKcId(user);
        if (string.IsNullOrWhiteSpace(callerKcId))
            return new RentalAccessFilter();

        IReadOnlyList<Guid> groupGuids = [];
        if (hasGroupScope)
        {
            groupGuids = [.. await authRepository.GetGroupGuidsForUserAsync(callerKcId, ct)];
        }

        return new RentalAccessFilter
        {
            OwnerKcId = hasOwnScope ? callerKcId : null,
            GroupGuids = groupGuids
        };
    }
}
