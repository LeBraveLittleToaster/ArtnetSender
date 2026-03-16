using LumenForgeServer.Auth.Domain;
using LumenForgeServer.Auth.Dto.Command;
using LumenForgeServer.Auth.Dto.Query;
using LumenForgeServer.Auth.Dto.Views;
using LumenForgeServer.Auth.Service;
using LumenForgeServer.Common.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace LumenForgeServer.Auth.Controller;

/// <summary>
/// HTTP API for managing users in the auth domain.
/// </summary>
/// <remarks>
/// Routes are under <c>api/v1/auth/user</c> and require authenticated access.
/// </remarks>
[Route("api/v1/auth/users")]
[ApiController]

public class UserController(UserService userService, KcService kcService, ILogger<UserController> _logger) : ControllerBase
{
    /// <summary>
    /// Lists local users with optional paging and search.
    /// </summary>
    /// <remarks>
    /// Example query: <c>GET /api/v1/auth/users?search=john&amp;limit=25&amp;offset=0</c>
    /// </remarks>
    /// <param name="query">Paging and search parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 200 response with the user list.</returns>
    [HttpGet("")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Authorize(Roles = nameof(Permissions.UserRead))]
    [Produces("application/json")]
    public async Task<IActionResult> ListUsers([FromQuery] ListQueryDto query, CancellationToken ct)
    {
        var users = await userService.ListUsers(query.Search, query.Limit, query.Offset, ct);
        return Ok(new ListViewDto<UserView>() { list = users.users, total = users.total });
    }

    /// <summary>
    /// Creates a user record for a Keycloak subject identifier.
    /// </summary>
    /// <param name="addKcUserDto">Payload containing the user information to create a new user.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 201 response with the created user payload.</returns>
    [HttpPut("")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Produces("application/json")]
    public async Task<IActionResult> RegisterNewUser([FromBody] AddKcUserDto addKcUserDto, CancellationToken ct)
    {
        var userKcId = await kcService.AddUserToKeycloak(addKcUserDto, ct);
        if (userKcId == null) throw new KeycloakException("User Id was not found");

        var user = await userService.AddUser(userKcId, addKcUserDto, ct);
        return CreatedAtAction(nameof(GetUser), new { userKcId = user?.UserKcId }, UserView.FromEntity(user!));
    }

    /// <summary>
    /// Retrieves a user by Keycloak subject identifier.
    /// </summary>
    /// <param name="include">Includes groupViews with include=groups, groups the user is assigned to.</param>
    /// <param name="userKcId">Keycloak subject identifier to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 200 response with the user payload.</returns>
    /// <exception cref="LumenForgeServer.Common.Exceptions.NotFoundException">
    /// Thrown when the user cannot be found.
    /// </exception>
    [HttpGet("{userKcId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Authorize(Policy = nameof(Policy.UserReadOrOwnProfile))]
    [Produces("application/json")]
    public async Task<IActionResult> GetUser(
        [FromQuery] string? include,
        [FromRoute, Required, MinLength(1), RegularExpression(@".*\S.*")]
        string userKcId,
        CancellationToken ct)
    {
        var includes = include?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(v => Enum.Parse<GetUserInclude>(v, true))
            .ToList();
        var withGroups = includes?.Contains(GetUserInclude.Groups) ?? false;

        var userView = await userService.GetUserByKeycloakId(userKcId, withGroups, ct);
        return new JsonResult(userView);
    }

    /// <summary>
    /// Retrieves groups assigned to a user.
    /// </summary>
    /// <param name="userKcId">Keycloak subject identifier to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 200 response with the group list.</returns>
    [HttpGet("{userKcId}/groups")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Policy = nameof(Policy.GroupRoleAndUserRead))]
    [Produces("application/json")]
    public async Task<IActionResult> GetUserGroups(
        [FromRoute, Required, MinLength(1), RegularExpression(@".*\S.*")]
        string userKcId,
        CancellationToken ct)
    {
        var groups = await userService.GetGroupsForUser(userKcId, ct);
        return Ok(groups);
    }

    /// <summary>
    /// Deletes a user by Keycloak subject identifier from the local database. User could be still present in keycloak.
    /// </summary>
    /// <param name="userKcId">Keycloak subject identifier to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 200 response with the user payload.</returns>
    /// <exception cref="LumenForgeServer.Common.Exceptions.NotFoundException">
    /// Thrown when the user cannot be found.
    /// </exception>
    [HttpDelete("{userKcId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Roles = nameof(Permissions.UserDelete))]
    [Produces("application/json")]
    public async Task<IActionResult> DeleteUserByKcId(
        [FromRoute, Required, MinLength(1), RegularExpression(@".*\S.*")]
        string userKcId,
        CancellationToken ct)
    {
        await userService.DeleteUserByKcId(userKcId, ct);
        return Ok();
    }

    /// <summary>
    /// Retrieves role assignments for the specified user.
    /// </summary>
    /// <param name="keycloakId">Keycloak subject identifier to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 200 response with the role set.</returns>
    [HttpGet("{keycloakId}/roles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Policy = nameof(Policy.GroupRoleAndUserRead))]
    [Produces("application/json")]
    public async Task<IActionResult> GetUserRoles(
        [FromRoute, Required, MinLength(1), RegularExpression(@".*\S.*")]
        string keycloakId,
        CancellationToken ct)
    {
        var roles = await userService.GetRolesForKcId(keycloakId, ct);
        return Ok(roles);
    }

    public enum GetUserInclude
    {
        Groups
    }
}


