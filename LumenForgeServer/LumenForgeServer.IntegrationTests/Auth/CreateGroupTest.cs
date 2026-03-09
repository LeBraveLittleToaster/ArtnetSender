// CreateGroupTest.cs
using FluentAssertions;
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
/// Integration tests for group CRUD and membership workflow behavior.
/// </summary>
[Collection(AuthCollection.Name)]
public class CreateGroupTest(AuthFixture fixture)
{
    [Fact]
    public async Task GET_groups_supports_search_and_paging()
    {
        var adminBundle = await fixture.GetInitialAdminUserAsync();
        var group = await CreateGroupAsync(adminBundle.AppClient);

        var resp = await adminBundle.AppClient.GetAsync(
            $"/api/v1/auth/groups?search={Uri.EscapeDataString(group.Name)}&limit=10&offset=0");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var groups = JsonSerializer.Deserialize<ListViewDto<GroupView>>(
            await resp.Content.ReadAsStringAsync(),
            Json.GetJsonSerializerOptions());

        groups.Should().NotBeNull();
        groups.list.Should().Contain(g => g.Guid == group.Guid);
    }

    private async Task<GroupView> CreateGroupAsync(HttpClient adminClient, string? name = null, string? description = null)
    {
        var groupName = name ?? "test Group " + Guid.NewGuid();
        var groupDesc = description ?? "test Group Description " + Guid.NewGuid() + " Extended";

        var response = await adminClient.PutAsJsonAsync("/api/v1/auth/groups", new AddGroupDto
        {
            Name = groupName,
            Description = groupDesc,
            Roles = []
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var groupView = await JsonSerializer.DeserializeAsync<GroupView>(
            await response.Content.ReadAsStreamAsync(),
            Json.GetJsonSerializerOptions());

        groupView.Should().NotBeNull();
        return groupView!;
    }

    [Fact]
    public async Task GET_groups_invalid_limit_returns_bad_request()
    {
        var adminBundle = await fixture.GetInitialAdminUserAsync();

        var resp = await adminBundle.AppClient.GetAsync("/api/v1/auth/groups?limit=0&offset=0");
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_new_group_creates_user()
    {
        var adminBundle = await fixture.GetInitialAdminUserAsync();

        var guid = Guid.NewGuid();
        var groupName = "My Test Name" + guid;
        var groupDesc = "My Test Description,My Test Description,My Test Description" + guid;

        var respPutClient = await adminBundle.AppClient.PutAsJsonAsync("/api/v1/auth/groups", new AddGroupDto
        {
            Name = groupName,
            Description = groupDesc,
            Roles = []
        });

        respPutClient.StatusCode.Should().Be(HttpStatusCode.Created);

        respPutClient = await adminBundle.AppClient.PutAsJsonAsync("/api/v1/auth/groups", new AddGroupDto
        {
            Name = groupName,
            Description = groupDesc,
            Roles = []
        });

        respPutClient.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task POST_new_group_query_and_delete()
    {
        var adminBundle = await fixture.GetInitialAdminUserAsync();

        var guid = Guid.NewGuid();
        var groupName = "My Test Name" + guid;
        var groupDesc = "My Test Description,My Test Description,My Test Description" + guid;

        var respPutClient = await adminBundle.AppClient.PutAsJsonAsync("/api/v1/auth/groups", new AddGroupDto
        {
            Name = groupName,
            Description = groupDesc,
            Roles = []
        });

        respPutClient.StatusCode.Should().Be(HttpStatusCode.Created);

        var groupView = await JsonSerializer.DeserializeAsync<GroupView>(
            await respPutClient.Content.ReadAsStreamAsync(),
            Json.GetJsonSerializerOptions());

        groupView.Should().NotBeNull();

        respPutClient = await adminBundle.AppClient.DeleteAsync($"/api/v1/auth/groups/{groupView!.Guid}");
        respPutClient.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var respGroupQuery = await adminBundle.AppClient.GetAsync($"/api/v1/auth/groups/{groupView.Guid}");
        respGroupQuery.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_group_invalid_payload_returns_bad_request()
    {
        var adminBundle = await fixture.GetInitialAdminUserAsync();

        var respMissingName = await adminBundle.AppClient.PutAsJsonAsync("/api/v1/auth/groups", new AddGroupDto
        {
            Name = " ",
            Description = "Valid description with enough length",
            Roles = []
        });

        respMissingName.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var respShortDesc = await adminBundle.AppClient.PutAsJsonAsync("/api/v1/auth/groups", new AddGroupDto
        {
            Name = "Valid Name " + Guid.NewGuid(),
            Description = "short",
            Roles = []
        });

        respShortDesc.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PATCH_group_updates_fields()
    {
        var adminBundle = await fixture.GetInitialAdminUserAsync();
        var group = await CreateGroupAsync(adminBundle.AppClient);

        var updateResp = await adminBundle.AppClient.PatchAsJsonAsync($"/api/v1/auth/groups/{group.Guid}", new UpdateGroupDto
        {
            Name = "Updated Name " + Guid.NewGuid(),
            Description = "Updated Description " + Guid.NewGuid() + " extended"
        });

        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = JsonSerializer.Deserialize<GroupView>(
            await updateResp.Content.ReadAsStringAsync(),
            Json.GetJsonSerializerOptions());

        updated.Should().NotBeNull();
        updated!.Name.Should().StartWith("Updated Name");
        updated.Description.Should().StartWith("Updated Description");
    }

    [Fact]
    public async Task PATCH_group_empty_body_returns_bad_request()
    {
        var adminBundle = await fixture.GetInitialAdminUserAsync();
        var group = await CreateGroupAsync(adminBundle.AppClient);

        var updateResp = await adminBundle.AppClient.PatchAsJsonAsync(
            $"/api/v1/auth/groups/{group.Guid}",
            new UpdateGroupDto());

        updateResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_group_invalid_guid_returns_not_found()
    {
        var adminBundle = await fixture.GetInitialAdminUserAsync();

        var resp = await adminBundle.AppClient.GetAsync("/api/v1/auth/groups/not-a-Guid");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_new_group_and_add_user()
    {
        var adminBundle = await fixture.GetInitialAdminUserAsync();
        var userBundle = await CreateNewUserAsync(fixture);

        var group = await CreateGroupAsync(adminBundle.AppClient);

        var groupGet = await adminBundle.AppClient.GetAsync($"/api/v1/auth/groups/{group.Guid}");
        groupGet.StatusCode.Should().Be(HttpStatusCode.OK);

        var respAssignUser = await fixture.AssignUserToGroupAsync(
            adminBundle.AppClient,
            group.Guid,
            userBundle.GetKcUserId(),
            adminBundle.GetKcUserId());

        respAssignUser.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static Task<TestUserBundle> CreateNewUserAsync(AuthFixture fixture)
    {
        var dto = CreateTestUserDto.CreateTestUser();
        return fixture.CreateNewUserAsync(dto);
    }
}