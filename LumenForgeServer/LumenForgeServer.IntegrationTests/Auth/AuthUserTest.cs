// AuthUserTest.cs
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
        var roles = JsonSerializer.Deserialize<List<Role>>(content, Json.GetJsonSerializerOptions());
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

    private static Task<TestUserBundle> CreateNewUserAsync(AuthFixture fixture)
    {
        var dto = CreateTestUserDto.CreateTestUser();
        return fixture.CreateNewUserAsync(dto);
    }
}