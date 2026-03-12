using LumenForgeServer.Auth.Domain;
using LumenForgeServer.Common.Database;
using LumenForgeServer.Common.Exceptions;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace LumenForgeServer.Auth.Persistence;

/// <summary>
/// EF Core-backed repository for auth users, groups, and roles.
/// </summary>
public sealed class AuthRepository(AppDbContext _db, ILogger<AuthRepository> _logger) : IAuthRepository

{

    /// <summary>
    /// Adds a new user and immediately saves the change.
    /// </summary>
    /// <param name="kcUserReference">User entity to persist.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="DbUpdateException">Thrown when persistence fails.</exception>
    public async Task AddUserAsync(KcUserReference kcUserReference, CancellationToken ct)
    {
        await _db.Users.AddAsync(kcUserReference, ct).AsTask();
    }

    /// <summary>
    /// Deletes a user by Keycloak subject identifier.
    /// </summary>
    /// <param name="userKcId">Keycloak subject identifier to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task DeleteUserByKcIdAsync(string userKcId, CancellationToken ct)
    {
        var user = await _db.Users
            .SingleOrDefaultAsync(u => u.UserKcId == userKcId, ct);
        if (user == null) throw new NotFoundException($"User with keycloakId {userKcId} not found");
        _db.Users.Remove(user);
    }

    /// <summary>
    /// Attempts to retrieve a user by Keycloak subject identifier.
    /// </summary>
    /// <param name="keycloakId">Keycloak subject identifier to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The user if found; otherwise <c>null</c>.</returns>
    public async Task<KcUserReference?> TryGetUserByKeycloakIdAsync(string keycloakId, CancellationToken ct)
    {
        return await _db.Users
            .Where(u => u.UserKcId == keycloakId)
            .SingleOrDefaultAsync(ct);
    }

    public async Task<KcUserReference?> TryGetUserByKeycloakIdWithGroupsAsync(string keycloakId, CancellationToken ct)
    {
        return await _db.Users
            .Where(u => u.UserKcId == keycloakId)
            .Include(u => u.GroupUsers)
            .ThenInclude(gu => gu.Group)
            .ThenInclude(g => g.GroupRoles)
            .SingleOrDefaultAsync(ct);
    }

    /// <summary>
    /// Lists users with optional paging and search.
    /// </summary>
    /// <param name="search">Optional search term.</param>
    /// <param name="limit">Maximum number of records to return.</param>
    /// <param name="offset">Number of records to skip.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of users.</returns>
    public async Task<(IReadOnlyList<KcUserReference> users, long total)> ListUsersAsync(string? search, int limit, int offset, CancellationToken ct)
    {
        IQueryable<KcUserReference> query = _db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u =>
                u.UserKcId.ToLower().Contains(search.ToLower()) ||
                u.UsernameMirror.ToLower().Contains(search.ToLower()));
        }

        var total = await query.LongCountAsync(ct);

        var users = await query
            .OrderBy(u => u.UserKcId)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(ct);

        return (users, total);
    }

    /// <summary>
    /// Attempts to retrieve a user and their group relationships by Keycloak subject identifier.
    /// </summary>
    /// <param name="keycloakId">Keycloak subject identifier to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The user if found; otherwise <c>null</c>.</returns>
    /// <remarks>
    /// This query does not currently include navigation properties.
    /// </remarks>
    public Task<KcUserReference?> TryGetUserAndGroupsByKeycloakIdAsync(string keycloakId, CancellationToken ct)
    {
        return _db.Users
            .Where(u => u.UserKcId == keycloakId)
            .SingleOrDefaultAsync(ct);
    }

    /// <summary>
    /// Retrieves all roles assigned to a user via group memberships.
    /// </summary>
    /// <param name="keycloakId">Keycloak subject identifier to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Distinct roles assigned to the user.</returns>
    public async Task<HashSet<Permissions>> GetRolesForKcIdAsync(string keycloakId, CancellationToken ct)
    {
        return await _db.Users
            .Where(u => u.UserKcId == keycloakId)
            .SelectMany(u => u.GroupUsers)
            .SelectMany(gu => gu.Group.GroupRoles)
            .Select(gr => gr.Permission)
            .Distinct()
            .ToHashSetAsync(ct);
    }

    /// <summary>
    /// Retrieves groups assigned to a user.
    /// </summary>
    /// <param name="keycloakId">Keycloak subject identifier to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Groups assigned to the user.</returns>
    public async Task<IReadOnlyList<Group>> GetGroupsForUserAsync(string keycloakId, CancellationToken ct)
    {
        var userId = await GetUserIdByKeycloakIdAsync(keycloakId, ct);
        return await _db.GroupUsers
            .AsNoTracking()
            .Where(gu => gu.UserId == userId)
            .Select(gu => gu.Group)
            .ToListAsync(ct);
    }



    /// <summary>
    /// Resolves the internal group id for a group Guid.
    /// </summary>
    /// <param name="groupGuid">Group Guid to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The internal group id.</returns>
    /// <exception cref="NotFoundException">Thrown when the group cannot be found.</exception>
    public async Task<long> GetGroupIdByGuidAsync(Guid groupGuid, CancellationToken ct)
    {
        var groupId = await _db.Groups
            .Where(g => g.Guid == groupGuid)
            .Select(g => g.Id)
            .SingleOrDefaultAsync(ct);

        return groupId == 0
            ? throw new NotFoundException($"Group {groupId} not found")
            : groupId;
    }

    public async Task<Group?> GetGroupByGuidWithPermissionsAsync(Guid groupGuid, CancellationToken ct)
    {
        var group = await _db.Groups
            .Where(g => g.Guid == groupGuid)
            .Include(g => g.GroupRoles)
            .SingleOrDefaultAsync(ct);

        return group
               ?? throw new NotFoundException($"Group {group} not found");
    }

    /// <summary>
    /// Resolves the group for a group Guid.
    /// </summary>
    /// <param name="groupGuid">Group Guid to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The group object.</returns>
    /// <exception cref="NotFoundException">Thrown when the group cannot be found.</exception>
    public async Task<Group?> GetGroupByGuidAsync(Guid groupGuid, CancellationToken ct)
    {
        var group = await _db.Groups
            .Where(g => g.Guid == groupGuid)
            .SingleOrDefaultAsync(ct);

        return group
               ?? throw new NotFoundException($"Group {group} not found");
    }

    /// <summary>
    /// Lists groups with optional paging and search.
    /// </summary>
    /// <param name="search">Optional search term.</param>
    /// <param name="limit">Maximum number of records to return.</param>
    /// <param name="offset">Number of records to skip.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of groups.</returns>
    public async Task<(IReadOnlyList<Group> groups, long total)> ListGroupsAsync(string? search, int limit, int offset, CancellationToken ct)
    {
        var query = _db.Groups.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(g =>
            g.Name.ToLower().Contains(search.ToLower()) || g.Description.ToLower().Contains(search.ToLower()));
        }

        var total = await query.CountAsync();

        var groups = await query.AsNoTracking()
            .OrderBy(g => g.Name)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(ct);
        return (groups, total);
    }

    /// <summary>
    /// Lists groups with optional paging and search and permissions included.
    /// </summary>
    /// <param name="search">Optional search term.</param>
    /// <param name="limit">Maximum number of records to return.</param>
    /// <param name="offset">Number of records to skip.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of groups.</returns>
    public async Task<(IReadOnlyList<Group> groups, long total)> ListGroupsWithPermissionsAsync(string? search, int limit, int offset, CancellationToken ct)
    {
        var query = _db.Groups.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(g =>
                g.Name.Contains(search) ||
                g.Description.Contains(search));
        }

        var total = await query.CountAsync(ct);

        var groups = await query.AsNoTracking()
            .Include(g => g.GroupRoles)
            .OrderBy(g => g.Name)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(ct);
        return (groups, total);
    }

    /// <summary>
    /// Resolves the internal user id for a Keycloak subject identifier.
    /// </summary>
    /// <param name="keycloakId">Keycloak subject identifier to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The internal user id.</returns>
    /// <exception cref="NotFoundException">Thrown when the user cannot be found.</exception>
    public async Task<long> GetUserIdByKeycloakIdAsync(string keycloakId, CancellationToken ct)
    {
        var userId = await _db.Users
            .Where(u => u.UserKcId == keycloakId)
            .Select(u => u.Id)
            .SingleOrDefaultAsync(ct);
        return userId == 0
            ? throw new NotFoundException($"User with keycloakId {keycloakId} not found")
            : userId;
    }

    /// <summary>
    /// Adds a new group to the persistence store.
    /// </summary>
    /// <param name="group">Group entity to persist.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task AddGroupAsync(Group group, CancellationToken ct)
    {
        await _db.Groups.AddAsync(group, ct).AsTask();
    }

    /// <summary>
    /// Deletes a group by Guid.
    /// </summary>
    /// <param name="guid">Group Guid to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    public Task DeleteGroupByGuidAsync(Guid guid, CancellationToken ct)
    {
        var groupsRange = _db.Groups
            .Where(g => g.Guid == guid);
        _db.Groups.RemoveRange(groupsRange);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Retrieves users assigned to a group.
    /// </summary>
    /// <param name="groupGuid">Group Guid to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Users assigned to the group.</returns>
    public async Task<(IReadOnlyList<KcUserReference> users, long total)> GetUsersForGroupAsync(Guid groupGuid, int limit, int offset, CancellationToken ct)
    {
        var groupId = await GetGroupIdByGuidAsync(groupGuid, ct);
        var users = await _db.GroupUsers
            .AsNoTracking()
            .Where(gu => gu.GroupId == groupId)
            .Select(gu => gu.KcUserReference)
            .ToListAsync(ct);
        var userPerGroupCount = await _db.GroupUsers
            .AsNoTracking()
            .Where(gu => gu.GroupId == groupId)
            .CountAsync(ct);
        return (users, userPerGroupCount);
    }

    /// <summary>
    /// Retrieves roles assigned to a group.
    /// </summary>
    /// <param name="groupGuid">Group Guid to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Roles assigned to the group.</returns>
    public async Task<IReadOnlyList<Permissions>> GetRolesForGroupAsync(Guid groupGuid, CancellationToken ct)
    {
        var groupId = await GetGroupIdByGuidAsync(groupGuid, ct);
        return await _db.GroupRoles
            .AsNoTracking()
            .Where(gr => gr.GroupId == groupId)
            .Select(gr => gr.Permission)
            .ToListAsync(ct);
    }

    public Task AssignRolesToGroupAsync(Group group, Permissions[] roles, CancellationToken ct)
    {
        if (group is null) throw new NotFoundException("Group not found");
        if (roles is null) throw new NotFoundException("Roles not found");
        var groupId = group.Id != 0 ? group.Id : throw new NotFoundException("GroupId not found");

        var groupRoles = roles.Distinct().Select(r => new GroupPermissions()
        {
            GroupId = groupId,
            Permission = r
        }).ToList();
        return _db.GroupRoles.AddRangeAsync(groupRoles, ct);
    }

    /// <summary>
    /// Removes a role assignment from a group.
    /// </summary>
    /// <param name="group">Group losing the role.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task RemoveAllRolesFromGroupAsync(Group group, CancellationToken ct)
    {
        var groupId = group.Id != 0 ? group.Id : throw new NotFoundException("Group not found");

        var groupRoles = await _db.GroupRoles.Where(gr => gr.GroupId == groupId).ToListAsync(ct);
        _db.GroupRoles.RemoveRange(groupRoles);
        _logger.LogInformation("Removed {GroupRolesCount} roles from the group {GroupName}", groupRoles.Count, group.Name);
    }

    /// <summary>
    /// Assigns a user to a group by Keycloak subject identifier and group Guid.
    /// </summary>
    /// <param name="assigneeKeycloakId">Optional Keycloak subject identifier for the actor performing the assignment.</param>
    /// <param name="keycloakId">Keycloak subject identifier for the user being assigned.</param>
    /// <param name="groupGuid">Target group Guid.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="NotFoundException">Thrown when the user or group cannot be found.</exception>
    public async Task AssignUserToGroupAsync(string? assigneeKeycloakId, string keycloakId, Guid groupGuid, CancellationToken ct)
    {

        var userId = await GetUserIdByKeycloakIdAsync(keycloakId, ct);
        var groupId = await GetGroupIdByGuidAsync(groupGuid, ct);

        if (userId == 0 || groupId == 0)
        {
            throw new NotFoundException(
                $"Uncatched error retrieving user with keycloakId {keycloakId} and groupId {groupId}");
        }

        await _db.GroupUsers.AddAsync(new GroupUser
        {
            AssignedByKeycloakId = assigneeKeycloakId,
            UserId = userId,
            GroupId = groupId,
            JoinedAt = SystemClock.Instance.GetCurrentInstant()
        }, ct);
    }

    /// <summary>
    /// Removes a user from a group.
    /// </summary>
    /// <param name="group">Group to remove the user from.</param>
    /// <param name="kcUserReference">User to remove.</param>
    /// <param name="ct">Cancellation token.</param>
    public Task RemoveUserFromGroupAsync(Group group, KcUserReference kcUserReference, CancellationToken ct)
    {
        var groupId = group.Id != 0 ? group.Id : throw new NotFoundException("Group not found");
        var userId = kcUserReference.Id != 0 ? kcUserReference.Id : throw new NotFoundException("User not found");
        return RemoveGroupUserAsync(groupId, userId, ct);
    }

    /// <summary>
    /// Checks whether a user is a member of a group.
    /// </summary>
    /// <param name="kcUserReference">User to check.</param>
    /// <param name="group">Group to check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if the user is in the group; otherwise <c>false</c>.</returns>
    public Task<bool> IsUserInGroupAsync(KcUserReference kcUserReference, Group group, CancellationToken ct)
    {
        var groupId = group.Id != 0 ? group.Id : throw new NotFoundException("Group not found");
        var userId = kcUserReference.Id != 0 ? kcUserReference.Id : throw new NotFoundException("User not found");
        return _db.GroupUsers
            .AsNoTracking()
            .AnyAsync(gu => gu.GroupId == groupId && gu.UserId == userId, ct);
    }

    /// <summary>
    /// Checks whether a group has a specific role assigned.
    /// </summary>
    /// <param name="group">Group to check.</param>
    /// <param name="permissions">Role to check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><c>true</c> if the role is assigned; otherwise <c>false</c>.</returns>
    public Task<bool> HasGroupRoleAsync(Group group, Permissions permissions, CancellationToken ct)
    {
        var groupId = group.Id != 0 ? group.Id : throw new NotFoundException("Group not found");
        return _db.GroupRoles
            .AsNoTracking()
            .AnyAsync(gr => gr.GroupId == groupId && gr.Permission == permissions, ct);
    }

    private async Task RemoveGroupUserAsync(long groupId, long userId, CancellationToken ct)
    {
        var groupUser = await _db.GroupUsers
            .SingleOrDefaultAsync(gu => gu.GroupId == groupId && gu.UserId == userId, ct);
        if (groupUser == null)
        {
            throw new NotFoundException($"User {userId} is not assigned to group {groupId}.");
        }

        _db.GroupUsers.Remove(groupUser);
    }

    /// <summary>
    /// Persists pending changes to the underlying data store.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="DbUpdateException">Thrown when persistence fails.</exception>
    public Task SaveChangesAsync(CancellationToken ct)
    {
        return _db.SaveChangesAsync(ct);
    }
}
