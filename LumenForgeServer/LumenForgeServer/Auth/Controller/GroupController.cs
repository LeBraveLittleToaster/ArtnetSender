using LumenForgeServer.Auth.Domain;
using LumenForgeServer.Auth.Dto;
using LumenForgeServer.Auth.Dto.Query;
using LumenForgeServer.Auth.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using LumenForgeServer.Auth.Dto.Command;
using LumenForgeServer.Auth.Dto.Views;

namespace LumenForgeServer.Auth.Controller;

/// <summary>
/// HTTP API for managing groups in the auth domain.
/// </summary>
/// <remarks>
/// Routes are under <c>api/v1/auth/group</c> and require authenticated access by REALM_ADMIN
/// </remarks>
[Route("api/v1/auth/groups")]
[ApiController]
[Authorize]
public class GroupController(GroupService groupService) : ControllerBase
{
    /// <summary>
    /// Lists groups with optional paging and search.
    /// </summary>
    /// <remarks>
    /// Example query: <c>GET /api/v1/auth/groups?search=ops&amp;limit=20&amp;offset=0</c>
    /// </remarks>
    /// <param name="query">Paging and search parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 200 response with the group list.</returns>
    [HttpGet("")]
    [Authorize(Roles = nameof(Role.GroupRead))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public async Task<IActionResult> ListGroups([FromQuery] ListQueryDto query, CancellationToken ct)
    {
        var groups = await groupService.ListGroups(query.Search, query.Limit, query.Offset, ct);
        return Ok(new ListViewDto<GroupView>(){list = groups.groups, total = groups.total});
    }

    /// <summary>
    /// Retrieves a group by groupGuid.
    /// </summary>
    /// <param name="groupGuid">Stable group Guid</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 200 response with the group view payload.</returns>
    /// <exception cref="LumenForgeServer.Common.Exceptions.NotFoundException">
    /// Thrown when the group cannot be found.
    /// </exception>
    [HttpGet("{groupGuid:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Authorize(Roles = nameof(Role.GroupRead))]
    [Produces("application/json")]
    public async Task<IActionResult> GetGroup([FromRoute] Guid groupGuid, CancellationToken ct)
    {
        var groupView = await groupService.GetGroupByGuid(groupGuid, ct);
        return new JsonResult(groupView);
    }

    /// <summary>
    /// Creates a group record.
    /// </summary>
    /// <param name="addUserDto">Payload containing the Keycloak subject identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 201 response with the created user payload.</returns>
    [HttpPut("")]
    [Authorize(Roles = nameof(Role.GroupCreate))]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Produces("application/json")]

    public async Task<IActionResult> AddGroup([FromBody] AddGroupDto dto, CancellationToken ct)
    {
        var group = await groupService.AddGroup(dto, ct);

        return CreatedAtAction(nameof(AddGroup), new { groupGuid = group }, group);
    }

    /// <summary>
    /// Updates a group record.
    /// </summary>
    /// <param name="groupGuid">Group guid to update.</param>
    /// <param name="dto">Payload containing updated group fields.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 200 response with the updated group payload.</returns>
    [HttpPatch("{groupGuid:guid}")]
    [Authorize(Roles = nameof(Role.GroupUpdate))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Produces("application/json")]
    public async Task<IActionResult> UpdateGroup([FromRoute] Guid groupGuid, [FromBody] UpdateGroupDto dto, CancellationToken ct)
    {
        var group = await groupService.UpdateGroup(groupGuid, dto, ct);
        return Ok(group);
    }

    /// <summary>
    /// Delete a group record, identified by Guid.
    /// </summary>
    /// <param name="groupGuid">Path var containing the group Guid.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 204 response with when deleted succesfully.</returns>
    [HttpDelete("{groupGuid:guid}")]
    [Authorize(Roles = nameof(Role.GroupDelete))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]

    public async Task<IActionResult> DeleteGroup([FromRoute] Guid groupGuid, CancellationToken ct)
    {
        await groupService.DeleteGroupByGuid(groupGuid, ct);

        return NoContent();
    }

    /// <summary>
    /// Assigns a user to a group. The user then inherits the role privileges of that group
    /// </summary>
    /// <param name="groupGuid">Group Guid the user is assigned to</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 200 response with when assigned successfully.</returns>
    [HttpPut("{groupGuid:guid}/users")]
    [Authorize(Roles = nameof(Role.GroupUpdate))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> AssignUserToGroup([FromBody] AssignUserToGroupDto dto, [FromRoute] Guid groupGuid, CancellationToken ct)
    {
        await groupService.AssignUserToGroup(dto.assigneeKcId, dto.userKcId, groupGuid, ct);
        
        return Ok();
    }

    /// <summary>
    /// Retrieves users assigned to a group.
    /// </summary>
    /// <param name="groupGuid">Group guid to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 200 response with the user list.</returns>
    [HttpGet("{groupGuid:guid}/users")]
    [Authorize(Policy = nameof(Policy.GroupRoleAndUserRead))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> GetGroupUsers([FromRoute] Guid groupGuid,[FromQuery] ListQueryDto query, CancellationToken ct)
    {
        var users = await groupService.GetUsersForGroup(groupGuid, query.Limit, query.Offset, ct);
        return Ok(new ListViewDto<UserView>(){list = users.users, total = users.total});
    }

    /// <summary>
    /// Removes a user from a group.
    /// </summary>
    /// <param name="groupGuid">Group guid to update.</param>
    /// <param name="userKcId">Keycloak subject identifier to remove.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 204 response when removed successfully.</returns>
    [HttpDelete("{groupGuid:guid}/users/{userKcId}")]
    [Authorize(Policy = nameof(Policy.GroupUpdateReadUser))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> RemoveUserFromGroup(
        [FromRoute] Guid groupGuid,
        [FromRoute, Required, MinLength(1), RegularExpression(@".*\S.*")]
        string userKcId,
        CancellationToken ct)
    {
        await groupService.RemoveUserFromGroup(groupGuid, userKcId, ct);
        return NoContent();
    }

    /// <summary>
    /// Retrieves roles assigned to a group.
    /// </summary>
    /// <param name="groupGuid">Group guid to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 200 response with the role list.</returns>
    [HttpGet("{groupGuid:guid}/roles")]
    [Authorize(Policy = nameof(Policy.GroupUpdateReadUser))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> GetGroupRoles([FromRoute] Guid groupGuid, CancellationToken ct)
    {
        var roles = await groupService.GetRolesForGroup(groupGuid, ct);
        return Ok(roles);
    }

    /// <summary>
    /// Assigns a role to a group after removing all others.
    /// </summary>
    /// <param name="groupGuid">Group guid to update.</param>
    /// <param name="dto">Roles to be assigned</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A 204 response when assigned successfully.</returns>
    [HttpPut("{groupGuid:guid}/roles")]
    [Authorize(Policy = nameof(Policy.GroupUpdateRoleRead))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Produces("application/json")]
    public async Task<IActionResult> AssignGroupRoles([FromRoute] Guid groupGuid, [FromBody] AssignGroupRolesDto dto, CancellationToken ct)
    {
        await groupService.AssignRolesToGroup(groupGuid, dto.Roles, ct);
        return NoContent();
    }
}