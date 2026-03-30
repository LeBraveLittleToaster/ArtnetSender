using LumenForgeServer.Auth.Domain;
using LumenForgeServer.Auth.Dto.Command;
using LumenForgeServer.Auth.Dto.Views;
using LumenForgeServer.Auth.Factory;
using LumenForgeServer.Auth.Persistence;
using LumenForgeServer.Common.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LumenForgeServer.Auth.Service;

/// <summary>
/// Application service for user-related auth operations.
/// </summary>
public class UserService(IAuthRepository authRepository, IMemoryCache cache)
{
   

    /// <summary>
    /// Retrieves a user by Keycloak subject identifier.
    /// </summary>
    /// <param name="keycloakId">Keycloak subject identifier to look up.</param>
    /// <param name="includeGroups">Groups the user is assigned to</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The user if found.</returns>
    /// <exception cref="NotFoundException">Thrown when the user cannot be found.</exception>
    public async Task<UserView?> GetUserByKeycloakId(string keycloakId, bool includeGroups, CancellationToken ct)
    {
        var user = includeGroups
            ? await authRepository.TryGetUserByKeycloakIdWithGroupsAsync(keycloakId, ct)
            : await authRepository.TryGetUserByKeycloakIdAsync(keycloakId, ct);
        if (user == null)
        {
            throw new NotFoundException($"User with Keycloak ID {keycloakId} not found.");
        }

        var effectivePermissions = await authRepository.GetRolesForKcIdAsync(keycloakId, ct);

        if (!includeGroups)
        {
            return UserView.FromEntity(user, effectivePermissions);
        }

        var groups = user.GroupUsers
            .Select(gu => GroupView.FromEntity(gu.Group))
            .ToList();

        return UserView.FromEntityWithGroups(user, groups, effectivePermissions);
    }

    /// <summary>
    /// Lists users with optional paging and search.
    /// </summary>
    /// <param name="search">Optional search term.</param>
    /// <param name="limit">Maximum number of records to return.</param>
    /// <param name="offset">Number of records to skip.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of users.</returns>
    public async Task<(IReadOnlyList<UserView> users, long total)> ListUsers(string? search, int limit, int offset, CancellationToken ct)
    {
        var users = await authRepository.ListUsersAsync(search, limit, offset, ct);
        return (users.users.Select(user => UserView.FromEntity(user)).ToList(), users.total);
    }

    /// <summary>
    /// Creates a user record from a payload.
    /// </summary>
    /// <param name="userKcId">Parameter mirror from keycloak user</param>
    /// <param name="dto">Request payload containing the input data required for the operation.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created user.</returns>
    /// <exception cref="ValidationException">Thrown when the payload fails validation.</exception>
    /// <exception cref="Microsoft.EntityFrameworkCore.DbUpdateException">Thrown when persistence fails.</exception>
    public async Task<KcUserReference?> AddUser(string userKcId, AddKcUserDto dto, CancellationToken ct)
    {
        var user = UserFactory.BuildUser(userKcId, dto.Username, dto.Email, dto.FirstName, dto.LastName);

        try
        {
            await authRepository.AddUserAsync(user, ct);
            await authRepository.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e)
        {
            throw new UniqueConstraintException(e.Message, e);
        }

        return user;
    }

    /// <summary>
    /// Updates a user's Keycloak subject identifier.
    /// </summary>
    /// <param name="userKcId">Current Keycloak subject identifier.</param>
    /// <param name="newUserKcId">New Keycloak subject identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated user.</returns>
    /// <exception cref="NotFoundException">Thrown when the user cannot be found.</exception>
    public async Task<UserView> UpdateUserKeycloakId(string userKcId, string newUserKcId, CancellationToken ct)
    {
        var user = await authRepository.TryGetUserByKeycloakIdAsync(userKcId, ct);
        if (user == null)
        {
            throw new NotFoundException($"User with Keycloak ID {userKcId} not found.");
        }

        if (!string.Equals(user.UserKcId, newUserKcId, StringComparison.Ordinal))
        {
            user.UserKcId = newUserKcId;
            try
            {
                await authRepository.SaveChangesAsync(ct);
            }
            catch (DbUpdateException e)
            {
                throw new UniqueConstraintException(e.Message, e);
            }
        }

        return UserView.FromEntity(user);
    }

    /// <summary>
    /// Deletes a user record from the database.
    /// </summary>
    /// <param name="userKcId">Stable Keycloak subject identifier</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created user.</returns>
    /// <exception cref="ValidationException">Thrown when the payload fails validation.</exception>
    public async Task DeleteUserByKcId(string userKcId, CancellationToken ct)
    {
        await authRepository.DeleteUserByKcIdAsync(userKcId, ct);
        await authRepository.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Retrieves all roles assigned to a user via group memberships.
    /// </summary>
    /// <param name="keycloakId">Keycloak subject identifier to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Distinct roles assigned to the user.</returns>
    public async Task<HashSet<Permissions>> GetRolesForKcId(string keycloakId, CancellationToken ct)
    {
        return await authRepository.GetRolesForKcIdAsync(keycloakId, ct);
    }

    /// <summary>
    /// Retrieves groups assigned to a user.
    /// </summary>
    /// <param name="keycloakId">Keycloak subject identifier to look up.</param>
    /// <param name="limit">Numeric input used by this operation.</param>
    /// <param name="offset">Numeric input used by this operation.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Groups assigned to the user.</returns>
    public async Task<(IReadOnlyList<GroupView> groups, long total)> GetGroupsForUser(string keycloakId, int limit, int offset, CancellationToken ct)
    {
        var (groups, total) = await authRepository.GetGroupsForUserAsync(keycloakId, limit, offset, ct);
        return (groups.Select(GroupView.FromEntity).ToList(), total);
    }
}
