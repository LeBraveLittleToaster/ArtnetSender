using System.Security.Claims;
using LumenForgeServer.Auth.Domain;
using LumenForgeServer.Auth.Persistence;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Dto.Query;

namespace LumenForgeServer.Rentals.Service;

/// <summary>
/// Builds caller-specific access scopes for rental read/create/update operations.
/// </summary>
public sealed class RentalAccessService(IAuthRepository authRepository)
{
    public async Task<RentalAccessFilter> BuildReadScopeAsync(ClaimsPrincipal user, CancellationToken ct)
        => await BuildScopeAsync(user, nameof(Permissions.RentalRead), ct);

    public async Task<RentalAccessFilter> BuildUpdateScopeAsync(ClaimsPrincipal user, CancellationToken ct)
        => await BuildScopeAsync(user, nameof(Permissions.RentalUpdate), ct);

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

    public static string? ResolveCallerKcId(ClaimsPrincipal user)
        => user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

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
