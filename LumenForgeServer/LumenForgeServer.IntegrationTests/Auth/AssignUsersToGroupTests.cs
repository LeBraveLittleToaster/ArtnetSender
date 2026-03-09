using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LumenForgeServer.Auth.Domain;
using LumenForgeServer.Auth.Dto;
using LumenForgeServer.Auth.Dto.Command;
using LumenForgeServer.Auth.Dto.Views;
using LumenForgeServer.Common;
using LumenForgeServer.IntegrationTests.Collections;
using LumenForgeServer.IntegrationTests.Fixtures;
using LumenForgeServer.IntegrationTests.Client;
using LumenForgeServer.IntegrationTests.TestSupport;

namespace LumenForgeServer.IntegrationTests.Auth;

/// <summary>
/// Integration tests for group membership and role assignment endpoints.
/// </summary>
[Collection(AuthCollection.Name)]
public class AssignUsersToGroupTests(AuthFixture fixture)
{
    [Fact]
    public async Task Assign_user_to_group_and_query_membership()
    {
        var adminBundle = await fixture.GetInitialAdminUserAsync();
        var userBundle = await CreateNewUserAsync(fixture);
        var group = await fixture.CreateGroupAsync(adminBundle.AppClient);

        var assignResp = await fixture.AssignUserToGroupAsync(
            adminBundle.AppClient,
            group.Guid,
            userBundle.GetKcUserId(),
            adminBundle.GetKcUserId());

        assignResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var usersResp = await adminBundle.AppClient.GetAsync($"/api/v1/auth/groups/{group.Guid}/users?limit=10&offset=0");
        usersResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var users = JsonSerializer.Deserialize<ListViewDto<UserView>>(
            await usersResp.Content.ReadAsStringAsync(),
            Json.GetJsonSerializerOptions());

        users.Should().NotBeNull();
        users!.list.Should().Contain(u => u.UserKcId == userBundle.GetKcUserId());
    }

    [Fact]
    public async Task Assign_user_to_group_twice_returns_conflict()
    {
        var adminBundle = await fixture.GetInitialAdminUserAsync();
        var userBundle = await CreateNewUserAsync(fixture);
        var group = await fixture.CreateGroupAsync(adminBundle.AppClient);

        var firstAssign = await fixture.AssignUserToGroupAsync(
            adminBundle.AppClient,
            group.Guid,
            userBundle.GetKcUserId(),
            adminBundle.GetKcUserId());
        firstAssign.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondAssign = await fixture.AssignUserToGroupAsync(
            adminBundle.AppClient,
            group.Guid,
            userBundle.GetKcUserId(),
            adminBundle.GetKcUserId());
        secondAssign.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Remove_user_from_group_then_not_listed()
    {
        var adminBundle = await fixture.GetInitialAdminUserAsync();
        var userBundle = await CreateNewUserAsync(fixture);
        var group = await fixture.CreateGroupAsync(adminBundle.AppClient);

        var assignResp = await fixture.AssignUserToGroupAsync(
            adminBundle.AppClient,
            group.Guid,
            userBundle.GetKcUserId(),
            adminBundle.GetKcUserId());
        assignResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var removeResp = await adminBundle.AppClient.DeleteAsync($"/api/v1/auth/groups/{group.Guid}/users/{userBundle.GetKcUserId()}");
        removeResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var usersResp = await adminBundle.AppClient.GetAsync($"/api/v1/auth/groups/{group.Guid}/users?limit=10&offset=0");
        usersResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var users = JsonSerializer.Deserialize<ListViewDto<UserView>>(
            await usersResp.Content.ReadAsStringAsync(),
            Json.GetJsonSerializerOptions());

        users.Should().NotBeNull();
        users!.list.Should().NotContain(u => u.UserKcId == userBundle.GetKcUserId());
    }

    [Fact]
    public async Task Assign_role_to_group_and_query_roles()
    {
        var adminBundle = await fixture.GetInitialAdminUserAsync();
        var group = await fixture.CreateGroupAsync(adminBundle.AppClient);
        var assignedRoles = new[] { Permissions.UserRead, Permissions.GroupRead };

        var assignResp = await adminBundle.AppClient.PutAsJsonAsync(
            $"/api/v1/auth/groups/{group.Guid}/roles",
            new AssignGroupRolesDto { Roles = assignedRoles });

        assignResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var rolesResp = await adminBundle.AppClient.GetAsync($"/api/v1/auth/groups/{group.Guid}/roles");
        rolesResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var roles = JsonSerializer.Deserialize<List<Permissions>>(
            await rolesResp.Content.ReadAsStringAsync(),
            Json.GetJsonSerializerOptions());

        roles.Should().NotBeNull();
        roles.Should().BeEquivalentTo(assignedRoles);
    }

    [Fact]
    public async Task User_roles_include_group_roles()
    {
        var adminBundle = await fixture.GetInitialAdminUserAsync();
        var userBundle = await CreateNewUserAsync(fixture);
        var group = await fixture.CreateGroupAsync(adminBundle.AppClient);
        var assignedRoles = new[] { Permissions.DeviceRead, Permissions.UserRead };

        var assignRolesResp = await adminBundle.AppClient.PutAsJsonAsync(
            $"/api/v1/auth/groups/{group.Guid}/roles",
            new AssignGroupRolesDto { Roles = assignedRoles });
        assignRolesResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var assignUserResp = await fixture.AssignUserToGroupAsync(
            adminBundle.AppClient,
            group.Guid,
            userBundle.GetKcUserId(),
            adminBundle.GetKcUserId());
        assignUserResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var userRolesResp = await adminBundle.AppClient.GetAsync($"/api/v1/auth/users/{userBundle.GetKcUserId()}/roles");
        userRolesResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var userRoles = JsonSerializer.Deserialize<List<Permissions>>(
            await userRolesResp.Content.ReadAsStringAsync(),
            Json.GetJsonSerializerOptions());

        userRoles.Should().NotBeNull();
        userRoles.Should().Contain(assignedRoles);
    }

    [Fact]
    public async Task Assign_role_twice_returns_conflict()
    {
        var adminBundle = await fixture.GetInitialAdminUserAsync();
        var group = await fixture.CreateGroupAsync(adminBundle.AppClient);
        var assignedRoles = new[] { Permissions.UserRead };

        var firstAssignResp = await adminBundle.AppClient.PutAsJsonAsync(
            $"/api/v1/auth/groups/{group.Guid}/roles",
            new AssignGroupRolesDto { Roles = assignedRoles });
        firstAssignResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var secondAssignResp = await adminBundle.AppClient.PutAsJsonAsync(
            $"/api/v1/auth/groups/{group.Guid}/roles",
            new AssignGroupRolesDto { Roles = assignedRoles });
        secondAssignResp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Remove_role_not_assigned_returns_not_found()
    {
        var adminBundle = await fixture.GetInitialAdminUserAsync();
        var group = await fixture.CreateGroupAsync(adminBundle.AppClient);

        var removeResp = await adminBundle.AppClient.DeleteAsync(
            $"/api/v1/auth/groups/{group.Guid}/roles/{Permissions.UserRead}");

        removeResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Assign_invalid_role_returns_bad_request()
    {
        var adminBundle = await fixture.GetInitialAdminUserAsync();
        var group = await fixture.CreateGroupAsync(adminBundle.AppClient);

        var assignResp = await adminBundle.AppClient.PutAsJsonAsync(
            $"/api/v1/auth/groups/{group.Guid}/roles",
            new { roles = new[] { "NOT_A_VALID_ROLE" } });

        assignResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Assign_user_not_found_returns_not_found()
    {
        var adminBundle = await fixture.GetInitialAdminUserAsync();
        var group = await fixture.CreateGroupAsync(adminBundle.AppClient);

        var assignResp = await fixture.AssignUserToGroupAsync(
            adminBundle.AppClient,
            group.Guid,
            Guid.NewGuid().ToString("N"),
            adminBundle.GetKcUserId());

        assignResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static Task<TestUserBundle> CreateNewUserAsync(AuthFixture fixture)
    {
        var dto = CreateTestUserDto.CreateTestUser();
        return fixture.CreateNewUserAsync(dto);
    }
}
