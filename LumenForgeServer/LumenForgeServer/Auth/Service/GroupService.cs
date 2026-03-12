using LumenForgeServer.Auth.Domain;
using LumenForgeServer.Auth.Dto.Command;
using LumenForgeServer.Auth.Dto.Views;
using LumenForgeServer.Auth.Factory;
using LumenForgeServer.Auth.Persistence;
using LumenForgeServer.Common.Exceptions;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace LumenForgeServer.Auth.Service;

/// <summary>
/// Application service for group-related auth operations.
/// </summary>
public class GroupService(IAuthRepository authRepository)
{

    /// <summary>
    /// Resolves a group by group Guid.
    /// </summary>
    /// <param name="guid">Group Guid to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The group object.</returns>
    /// <exception cref="NotFoundException">Thrown when the group cannot be found.</exception>
    public async Task<GroupView> GetGroupByGuid(Guid guid, bool withPermissions, CancellationToken ct)
    {
        var group = withPermissions
            ? await authRepository.GetGroupByGuidWithPermissionsAsync(guid, ct)
            : await authRepository.GetGroupByGuidAsync(guid, ct);
        return group == null ? throw new NotFoundException("Group not found") : GroupView.FromEntity(group);
    }

    /// <summary>
    /// Lists groups with optional paging and search.
    /// </summary>
    /// <param name="search">Optional search term.</param>
    /// <param name="limit">Maximum number of records to return.</param>
    /// <param name="offset">Number of records to skip.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of groups.</returns>
    public async Task<(IReadOnlyList<GroupView> groups, long total)> ListGroups(string? search, int limit, int offset, bool withPermissions, CancellationToken ct)
    {
        var groups = withPermissions
            ? await authRepository.ListGroupsAsync(search, limit, offset, ct)
            : await authRepository.ListGroupsWithPermissionsAsync(search, limit, offset, ct);
        return (groups.groups.Select(GroupView.FromEntity).ToList(), groups.total);
    }

    /// <summary>
    /// Resolves a group id by group Guid.
    /// </summary>
    /// <param name="guid">Group Guid to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The internal group id.</returns>
    /// <exception cref="NotFoundException">Thrown when the group cannot be found.</exception>
    public async Task<long> GetGroupIdByGuid(Guid guid, CancellationToken ct)
    {
        var groupId = await authRepository.GetGroupIdByGuidAsync(guid, ct);
        return groupId;
    }

    /// <summary>
    /// Creates a group record from a payload.
    /// </summary>
    /// <param name="dto">Payload containing the name, description and roles.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created group.</returns>
    /// <exception cref="ValidationException">Thrown when the payload fails validation.</exception>
    /// <exception cref="Microsoft.EntityFrameworkCore.DbUpdateException">Thrown when persistence fails.</exception>
    public async Task<GroupView> AddGroup(AddGroupDto dto, CancellationToken ct)
    {
        var group = GroupFactory.BuildGroup(dto);

        try
        {
            await authRepository.AddGroupAsync(group, ct);
            await authRepository.SaveChangesAsync(ct);
            await authRepository.AssignRolesToGroupAsync(group, dto.Roles, ct);
            await authRepository.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e)
        {
            throw new UniqueConstraintException(e.Message, e);
        }


        return GroupView.FromEntity(group);
    }

    /// <summary>
    /// Updates a group record.
    /// </summary>
    /// <param name="groupGuid">Group Guid to update.</param>
    /// <param name="dto">Payload containing updated group fields.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated group.</returns>
    public async Task<GroupView> UpdateGroup(Guid groupGuid, UpdateGroupDto dto, CancellationToken ct)
    {
        var group = await authRepository.GetGroupByGuidAsync(groupGuid, ct)
            ?? throw new NotFoundException("Group not found");

        if (!string.IsNullOrWhiteSpace(dto.Name))
        {
            group.Name = dto.Name;
        }

        if (!string.IsNullOrWhiteSpace(dto.Description))
        {
            group.Description = dto.Description;
        }

        group.UpdatedAt = SystemClock.Instance.GetCurrentInstant();

        try
        {
            await authRepository.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e)
        {
            throw new UniqueConstraintException(e.Message, e);
        }

        return GroupView.FromEntity(group);
    }

    public async Task DeleteGroupByGuid(Guid parsedGroupGuid, CancellationToken ct)
    {
        try
        {
            await authRepository.DeleteGroupByGuidAsync(parsedGroupGuid, ct);
            await authRepository.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e)
        {
            throw new NotFoundException(e.Message);
        }
    }

    public async Task AssignUserToGroup(string? assigneeKcId, string userKcId, Guid groupGuid, CancellationToken ct)
    {
        try
        {
            await authRepository.AssignUserToGroupAsync(assigneeKcId, userKcId, groupGuid, ct);
            await authRepository.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e)
        {
            throw new UniqueConstraintException(e.Message, e);
        }
    }

    /// <summary>
    /// Retrieves users assigned to a group.
    /// </summary>
    /// <param name="groupGuid">Group Guid to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Users assigned to the group.</returns>
    public async Task<(IReadOnlyList<UserView> users, long total)> GetUsersForGroup(Guid groupGuid, int limit, int offset, CancellationToken ct)
    {
        var users = await authRepository.GetUsersForGroupAsync(groupGuid, limit, offset, ct);
        return (users.users.Select(UserView.FromEntity).ToList(), users.total);
    }

    /// <summary>
    /// Removes a user from a group.
    /// </summary>
    /// <param name="groupGuid">Group Guid to update.</param>
    /// <param name="userKcId">Keycloak subject identifier to remove.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task RemoveUserFromGroup(Guid groupGuid, string userKcId, CancellationToken ct)
    {
        var group = await authRepository.GetGroupByGuidAsync(groupGuid, ct)
            ?? throw new NotFoundException("Group not found");
        var user = await authRepository.TryGetUserByKeycloakIdAsync(userKcId, ct);
        if (user == null)
        {
            throw new NotFoundException($"User with Keycloak ID {userKcId} not found.");
        }

        await authRepository.RemoveUserFromGroupAsync(group, user, ct);
        await authRepository.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Retrieves roles assigned to a group.
    /// </summary>
    /// <param name="groupGuid">Group Guid to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Roles assigned to the group.</returns>
    public async Task<IReadOnlyList<Permissions>> GetRolesForGroup(Guid groupGuid, CancellationToken ct)
    {
        return await authRepository.GetRolesForGroupAsync(groupGuid, ct);
    }

    /// <summary>
    /// Removes all existing roles and assigns the given roles to the group.
    /// </summary>
    /// <param name="groupGuid">Group Guid to update.</param>
    /// <param name="roles">Roles to assign.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task AssignRolesToGroup(Guid groupGuid, Permissions[] roles, CancellationToken ct)
    {
        var group = await authRepository.GetGroupByGuidAsync(groupGuid, ct)
            ?? throw new NotFoundException("Group not found");
        try
        {
            await authRepository.RemoveAllRolesFromGroupAsync(group, ct);
            await authRepository.AssignRolesToGroupAsync(group, roles, ct);
            await authRepository.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e)
        {
            throw new UniqueConstraintException(e.Message, e);
        }
    }
}
