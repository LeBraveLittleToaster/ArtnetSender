// AuthUserTest.cs
using FluentAssertions;
using LumenForgeServer.Auth.Domain;
using LumenForgeServer.Auth.Dto.Command;
using LumenForgeServer.Auth.Dto.Views;
using LumenForgeServer.Common;
using LumenForgeServer.IntegrationTests.Collections;
using LumenForgeServer.IntegrationTests.Fixtures;
using LumenForgeServer.IntegrationTests.TestSupport;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace LumenForgeServer.IntegrationTests.Auth;

/// <summary>
/// Integration tests for user query and deletion endpoints in the auth API.
/// </summary>
[Collection(AuthCollection.Name)]
public class AuthUserTest(AuthFixture fixture)
{
    [Fact]
    public async Task GET_users_supports_search_and_paging()
    {
        var adminBundle = await fixture.GetInitialAdminUserAsync();
        var userBundle = await CreateNewUserAsync(fixture);

        var userKcId = userBundle.GetKcUserId();
        var resp = await adminBundle.AppClient.GetAsync($"/api/v1/auth/users?search={userKcId}&limit=10&offset=0");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await resp.Content.ReadAsStringAsync();
        var users = JsonSerializer.Deserialize<ListViewDto<UserView>>(content, Json.GetJsonSerializerOptions());
        users.Should().NotBeNull();
        users.list.Should().Contain(u => u.UserKcId == userKcId);
    }

    [Fact]
    public async Task GET_users_invalid_limit_returns_bad_request()
    {
        var adminBundle = await fixture.GetInitialAdminUserAsync();
        var resp = await adminBundle.AppClient.GetAsync("/api/v1/auth/users?limit=0&offset=0");

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_user_invalid_payload_returns_bad_request()
    {
        var adminBundle = await fixture.GetInitialAdminUserAsync();

        var resp = await adminBundle.AppClient.PutAsJsonAsync("/api/v1/auth/users", new AddKcUserDto
        {
            Username = " ",
            Password = "Password" + Guid.NewGuid(),
            Email = "test-" + Guid.NewGuid() + "@test.de",
            FirstName = "Test",
            LastName = "User",
            Groups = [],
            RealmRoles = []
        });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_user_roles_empty_when_user_has_no_groups()
    {
        var adminUser = await fixture.GetInitialAdminUserAsync();
        var testUser = CreateTestUserDto.CreateTestUser();
        var userBundle = await fixture.CreateNewUserAsync(testUser);

        var userKcId = userBundle.GetKcUserId();
        var resp = await adminUser.AppClient.GetAsync($"/api/v1/auth/users/{userKcId}/roles");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await resp.Content.ReadAsStringAsync();
        var roles = JsonSerializer.Deserialize<List<Permissions>>(content, Json.GetJsonSerializerOptions());
        roles.Should().NotBeNull();
        roles.Should().BeEmpty();
    }

    [Fact]
    public async Task GET_user_not_found_returns_not_found()
    {
        var adminBundle = await fixture.GetInitialAdminUserAsync();
        var resp = await adminBundle.AppClient.GetAsync($"/api/v1/auth/users/{Guid.NewGuid()}");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_user_include_groups_returns_assigned_groups()
    {
        var adminBundle = await fixture.GetInitialAdminUserAsync();
        var userBundle = await CreateNewUserAsync(fixture);
        var userKcId = userBundle.GetKcUserId();

        var groupA = await fixture.CreateGroupAsync(
            adminBundle.AppClient,
            $"Include Groups A {Guid.NewGuid()}",
            $"Include Groups Description A {Guid.NewGuid()}");
        var groupB = await fixture.CreateGroupAsync(
            adminBundle.AppClient,
            $"Include Groups B {Guid.NewGuid()}",
            $"Include Groups Description B {Guid.NewGuid()}");

        var assignGroupA = await fixture.AssignUserToGroupAsync(
            adminBundle.AppClient,
            groupA.Guid,
            userKcId,
            adminBundle.GetKcUserId());
        var assignGroupB = await fixture.AssignUserToGroupAsync(
            adminBundle.AppClient,
            groupB.Guid,
            userKcId,
            adminBundle.GetKcUserId());

        assignGroupA.StatusCode.Should().Be(HttpStatusCode.OK);
        assignGroupB.StatusCode.Should().Be(HttpStatusCode.OK);

        var resp = await adminBundle.AppClient.GetAsync($"/api/v1/auth/users/{userKcId}?include=groups");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var groups = json.RootElement.GetProperty("groups");

        groups.ValueKind.Should().Be(JsonValueKind.Array);
        groups.GetArrayLength().Should().Be(2);

        var returnedGroups = groups.EnumerateArray().Select(g => new
        {
            Guid = g.GetProperty("guid").GetGuid(),
            Name = g.GetProperty("name").GetString()
        }).ToList();

        returnedGroups.Select(g => g.Guid).Should().BeEquivalentTo([groupA.Guid, groupB.Guid]);
        returnedGroups.Select(g => g.Name).Should().BeEquivalentTo([groupA.Name, groupB.Name]);
    }

    [Fact]
    public async Task DELETE_user_removes_local_record()
    {
        var adminBundle = await fixture.GetInitialAdminUserAsync();
        var userBundle = await CreateNewUserAsync(fixture);
        var userKcId = userBundle.GetKcUserId();

        var deleteResp = await adminBundle.AppClient.DeleteAsync($"/api/v1/auth/users/{userKcId}");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResp = await adminBundle.AppClient.GetAsync($"/api/v1/auth/users/{userKcId}");
        getResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_user_allows_own_profile_without_userread_role()
    {
        var userBundle = await CreateNewUserAsync(fixture);
        var ownKcId = userBundle.GetKcUserId();

        var resp = await userBundle.AppClient.GetAsync($"/api/v1/auth/users/{ownKcId}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await resp.Content.ReadAsStringAsync();
        var userView = JsonSerializer.Deserialize<UserView>(content, Json.GetJsonSerializerOptions());
        userView.Should().NotBeNull();
        userView!.UserKcId.Should().Be(ownKcId);
    }

    [Fact]
    public async Task GET_user_without_userread_cannot_read_foreign_profile()
    {
        var userA = await CreateNewUserAsync(fixture);
        var userB = await CreateNewUserAsync(fixture);

        var foreignKcId = userB.GetKcUserId();
        var resp = await userA.AppClient.GetAsync($"/api/v1/auth/users/{foreignKcId}");

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GET_user_with_userread_role_can_read_foreign_profile()
    {
        var reader = await fixture.CreateNewUserWithRolesAsync(
            CreateTestUserDto.CreateTestUser(),
            [Permissions.UserRead]);

        var foreign = await CreateNewUserAsync(fixture);
        var foreignKcId = foreign.GetKcUserId();

        var resp = await reader.AppClient.GetAsync($"/api/v1/auth/users/{foreignKcId}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_user_returns_effective_permissions_and_rental_scope_for_own_role()
    {
        var user = await fixture.CreateNewUserWithRolesAsync(
            CreateTestUserDto.CreateTestUser(),
            [Permissions.RentalUserOwn]);

        var ownKcId = user.GetKcUserId();
        var resp = await user.AppClient.GetAsync($"/api/v1/auth/users/{ownKcId}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var permissionsElement = json.RootElement.GetProperty("effective_permissions");
        var rentalScopesElement = json.RootElement.GetProperty("rental_scopes");
        var permissions = permissionsElement.Deserialize<List<Permissions>>(Json.GetJsonSerializerOptions());
        var rentalScopes = rentalScopesElement.Deserialize<RentalScopesView>(Json.GetJsonSerializerOptions());

        permissions.Should().NotBeNull();
        rentalScopes.Should().NotBeNull();
        permissions!.Should().Contain(Permissions.RentalUserOwn);
        rentalScopes!.Read.Should().Be(ScopeLevel.Own);
        rentalScopes.Create.Should().Be(ScopeLevel.Own);
        rentalScopes.Update.Should().Be(ScopeLevel.Own);
        rentalScopes.Delete.Should().Be(ScopeLevel.None);
    }

    [Fact]
    public async Task GET_user_returns_combined_group_and_own_rental_scope()
    {
        var user = await fixture.CreateNewUserWithRolesAsync(
            CreateTestUserDto.CreateTestUser(),
            [Permissions.RentalUserOwn, Permissions.RentalGroup]);

        var ownKcId = user.GetKcUserId();
        var resp = await user.AppClient.GetAsync($"/api/v1/auth/users/{ownKcId}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var json = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var permissionsElement = json.RootElement.GetProperty("effective_permissions");
        var rentalScopesElement = json.RootElement.GetProperty("rental_scopes");
        var permissions = permissionsElement.Deserialize<List<Permissions>>(Json.GetJsonSerializerOptions());
        var rentalScopes = rentalScopesElement.Deserialize<RentalScopesView>(Json.GetJsonSerializerOptions());

        permissions.Should().NotBeNull();
        rentalScopes.Should().NotBeNull();
        permissions!.Should().Contain(Permissions.RentalUserOwn);
        permissions.Should().Contain(Permissions.RentalGroup);
        rentalScopes!.Read.Should().Be(ScopeLevel.OwnAndGroup);
        rentalScopes.Create.Should().Be(ScopeLevel.OwnAndGroup);
        rentalScopes.Update.Should().Be(ScopeLevel.OwnAndGroup);
        rentalScopes.Delete.Should().Be(ScopeLevel.None);
    }

    private static Task<TestUserBundle> CreateNewUserAsync(AuthFixture fixture)
    {
        var dto = CreateTestUserDto.CreateTestUser();
        return fixture.CreateNewUserAsync(dto);
    }
}
