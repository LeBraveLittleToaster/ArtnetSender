using LumenForgeServer.Auth.Domain;

namespace LumenForgeServer.Auth.Persistance;

/// <summary>
/// Persistence contract for auth users, groups, and roles.
/// </summary>
public interface IAuthRepository
{
    /// <summary>
    /// Resolves the internal user id for a Keycloak subject identifier.
    /// </summary>
    /// <param name="keycloakId">Keycloak subject identifier to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The internal user id.</returns>
    /// <exception cref="LumenForgeServer.Common.Exceptions.NotFoundException">
    /// Thrown when the user cannot be found.
    /// </exception>
    Task<long> GetUserIdByKeycloakIdAsync(string keycloakId, CancellationToken ct);
    /// <summary>
    /// Adds a user to the persistence store.
    /// </summary>
    /// <param name="kcUserReference">User entity to persist.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddUserAsync(KcUserReference kcUserReference, CancellationToken ct);
    /// <summary>
    /// Deletes a user by Keycloak subject identifier.
    /// </summary>
    /// <param name="keycloakId">Keycloak subject identifier to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="LumenForgeServer.Common.Exceptions.NotFoundException">
    /// Thrown when the user cannot be found.
    /// </exception>
    Task DeleteUserByKcIdAsync(string userKcId, CancellationToken ct);
    /// <summary>
    /// Attempts to retrieve a user by Keycloak subject identifier.
    /// </summary>
    /// <param name="keycloakId">Keycloak subject identifier to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The user if found; otherwise <c>null</c>.</returns>
    Task<KcUserReference?> TryGetUserByKeycloakIdAsync(string keycloakId, CancellationToken ct);

    /// <summary>
    /// Attempts to retrieve a user by Keycloak subject identifier including the groups the user assigned to.
    /// </summary>
    /// <param name="keycloakId">Keycloak subject identifier to look up.</param>
    /// <param name="includeGroups">Groups the user is assigned to</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The user if found; otherwise <c>null</c>.</returns>
    public Task<KcUserReference?> TryGetUserByKeycloakIdWithGroupsAsync(string keycloakId,
        CancellationToken ct);
    /// <summary>
    /// Lists users with optional paging and search.
    /// </summary>
    /// <param name="search">Optional search term.</param>
    /// <param name="limit">Maximum number of records to return.</param>
    /// <param name="offset">Number of records to skip.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of users.</returns>
    Task<(IReadOnlyList<KcUserReference> users, long total)> ListUsersAsync(string? search, int limit, int offset, CancellationToken ct);
    /// <summary>
    /// Retrieves all roles assigned to a user via group memberships.
    /// </summary>
    /// <param name="keycloakId">Keycloak subject identifier to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Distinct roles assigned to the user.</returns>
    Task<HashSet<Permissions>> GetRolesForKcIdAsync(string keycloakId, CancellationToken ct);
    /// <summary>
    /// Retrieves groups assigned to a user.
    /// </summary>
    /// <param name="keycloakId">Keycloak subject identifier to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Groups assigned to the user.</returns>
    Task<IReadOnlyList<Group>> GetGroupsForUserAsync(string keycloakId, CancellationToken ct);
    
    
    /// <summary>
    /// Resolves the group for a group guid.
    /// </summary>
    /// <param name="groupGuid">Group guid to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The group object.</returns>
    /// <exception cref="LumenForgeServer.Common.Exceptions.NotFoundException">
    /// Thrown when the group cannot be found.
    /// </exception>
    Task<Group?> GetGroupByGuidAsync(Guid groupGuid, CancellationToken ct);
    /// <summary>
    /// Lists groups with optional paging and search.
    /// </summary>
    /// <param name="search">Optional search term.</param>
    /// <param name="limit">Maximum number of records to return.</param>
    /// <param name="offset">Number of records to skip.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of groups.</returns>
    Task<(IReadOnlyList<Group> groups, long total)> ListGroupsAsync(string? search, int limit, int offset, CancellationToken ct);
    /// <summary>
    /// Resolves the internal group id for a group guid.
    /// </summary>
    /// <param name="groupGuid">Group guid to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The internal group id.</returns>
    /// <exception cref="LumenForgeServer.Common.Exceptions.NotFoundException">
    /// Thrown when the group cannot be found.
    /// </exception>
    Task<long> GetGroupIdByGuidAsync(Guid groupGuid, CancellationToken ct);
    /// <summary>
    /// Adds a group to the persistence store.
    /// </summary>
    /// <param name="group">Group entity to persist.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddGroupAsync(Group group, CancellationToken ct);
    /// <summary>
    /// Deletes a group by guid.
    /// </summary>
    /// <param name="guid">Group guid to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="LumenForgeServer.Common.Exceptions.NotFoundException">
    /// Thrown when the group cannot be found.
    /// </exception>
    Task DeleteGroupByGuidAsync(Guid guid, CancellationToken ct);
    /// <summary>
    /// Retrieves users assigned to a group.
    /// </summary>
    /// <param name="groupGuid">Group guid to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Users assigned to the group.</returns>
    Task<(IReadOnlyList<KcUserReference> users, long total)> GetUsersForGroupAsync(Guid groupGuid, int limit, int offset, CancellationToken ct);
    /// <summary>
    /// Retrieves roles assigned to a group.
    /// </summary>
    /// <param name="groupGuid">Group guid to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Roles assigned to the group.</returns>
    Task<IReadOnlyList<Permissions>> GetRolesForGroupAsync(Guid groupGuid, CancellationToken ct);
    
    /// <summary>
    /// Assigns multiple roles to a group.
    /// </summary>
    /// <param name="group">Group receiving the role.</param>
    /// <param name="roles">Array of Roles to assign.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AssignRolesToGroupAsync(Group group, Permissions[] roles, CancellationToken ct);
    /// <summary>
    /// Removes a role assignment from a group.
    /// </summary>
    /// <param name="group">Group losing the role.</param>
    /// <param name="role">Role to remove.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RemoveAllRolesFromGroupAsync(Group group, CancellationToken ct);
    
    /// <summary>
    /// Assigns a user to a group.
    /// </summary>
    /// <param name="assigneeKeycloakId">Optional Keycloak subject identifier for the actor performing the assignment.</param>
    /// <param name="keycloakId">Keycloak subject identifier for the user being assigned.</param>
    /// <param name="groupGuid">Target group guid.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="LumenForgeServer.Common.Exceptions.NotFoundException">
    /// Thrown when the user or group cannot be found.
    /// </exception>
    Task AssignUserToGroupAsync(string? assigneeKeycloakId, string keycloakId, Guid groupGuid, CancellationToken ct);
    /// <summary>
    /// Removes a user from a group.
    /// </summary>
    /// <param name="group">Group to remove the user from.</param>
    /// <param name="kcUserReference">User to remove.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RemoveUserFromGroupAsync(Group group, KcUserReference kcUserReference, CancellationToken ct);
    
    /// <summary>
    /// Checks whether a user is a member of a group.
    /// </summary>
    /// <param name="kcUserReference">User to check.</param>
    /// <param name="group">Group to check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if the user is in the group; otherwise <c>false</c>.</returns>
    Task<bool> IsUserInGroupAsync(KcUserReference kcUserReference, Group group, CancellationToken ct);
    /// <summary>
    /// Checks whether a group has a specific role assigned.
    /// </summary>
    /// <param name="group">Group to check.</param>
    /// <param name="permissions">Role to check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if the role is assigned; otherwise <c>false</c>.</returns>
    Task<bool> HasGroupRoleAsync(Group group, Permissions permissions, CancellationToken ct);

    /// <summary>
    /// Persists pending changes to the underlying data store.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task SaveChangesAsync(CancellationToken ct);
}
