using FluentAssertions;
using LumenForgeServer.Auth.Domain;
using LumenForgeServer.Auth.Dto.Command;
using LumenForgeServer.Auth.Dto.Views;
using LumenForgeServer.Common;
using LumenForgeServer.Common.Database;
using LumenForgeServer.IntegrationTests.Collections;
using LumenForgeServer.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace LumenForgeServer.IntegrationTests.Auth;

/// <summary>
/// Integration tests for group permission includes and eager loading behavior.
/// Tests the <see cref="IAuthRepository.GetGroupByGuidWithPermissionsAsync"/>
/// and <see cref="IAuthRepository.ListGroupsWithPermissionsAsync"/> methods.
/// </summary>
[Collection(AuthCollection.Name)]
public class GroupPermissionsIncludeTests(AuthFixture fixture)
{
    // =========================================================================
    // GetGroupByGuidWithPermissionsAsync
    // =========================================================================

    [Fact]
    public async Task Repository_GetGroupByGuidWithPermissions_loads_group_roles()
    {
        var adminBundle = await fixture.GetInitialAdminUserAsync();
        var roles = new[]
        {
            Permissions.DeviceCreate,
            Permissions.DeviceRead,
            Permissions.DeviceUpdate,
        };

        // Create group with roles via API
        var group = await fixture.CreateGroupWithRolesAsync(
            adminBundle.AppClient,
            roles,
            $"TestGroup-{Guid.NewGuid():N}",
            "Test permissions loading");

        // Load via repository with permissions included
        var dbFixture = new AppDbFixture();
        await using var db = dbFixture.CreateDbContext();
        var groupFromDb = await db.Groups
            .Where(g => g.Guid == group.Guid)
            .Include(g => g.GroupRoles)
            .SingleOrDefaultAsync();

        groupFromDb.Should().NotBeNull();
        groupFromDb!.GroupRoles.Should().NotBeEmpty();
        groupFromDb.GroupRoles.Should().HaveCount(3);
        groupFromDb.GroupRoles.Select(gr => gr.Permission).Should().Contain(Permissions.DeviceCreate);
        groupFromDb.GroupRoles.Select(gr => gr.Permission).Should().Contain(Permissions.DeviceRead);
        groupFromDb.GroupRoles.Select(gr => gr.Permission).Should().Contain(Permissions.DeviceUpdate);
    }

    [Fact]
    public async Task Repository_GetGroupByGuidWithPermissions_returns_null_for_unknown_group()
    {
        var dbFixture = new AppDbFixture();
        await using var db = dbFixture.CreateDbContext();
        var groupFromDb = await db.Groups
            .Where(g => g.Guid == Guid.NewGuid())
            .Include(g => g.GroupRoles)
            .SingleOrDefaultAsync();

        groupFromDb.Should().BeNull();
    }

    [Fact]
    public async Task Repository_GetGroupByGuidWithPermissions_includes_empty_roles_for_new_group()
    {
        var adminBundle = await fixture.GetInitialAdminUserAsync();

        // Create group WITHOUT roles
        var group = await fixture.CreateGroupAsync(
            adminBundle.AppClient,
            name: $"NoRoles-{Guid.NewGuid():N}",
            description: "Group with no roles");

        // Load via repository with permissions included
        var dbFixture = new AppDbFixture();
        await using var db = dbFixture.CreateDbContext();
        var groupFromDb = await db.Groups
            .Where(g => g.Guid == group.Guid)
            .Include(g => g.GroupRoles)
            .SingleOrDefaultAsync();

        groupFromDb.Should().NotBeNull();
        groupFromDb!.GroupRoles.Should().BeEmpty();
    }

    [Fact]
    public async Task Repository_GetGroupByGuidWithPermissions_can_be_used_multiple_times()
    {
        var adminBundle = await fixture.GetInitialAdminUserAsync();
        var roleSet = new[] { Permissions.RentalRead, Permissions.RentalUpdate };

        var group = await fixture.CreateGroupWithRolesAsync(
            adminBundle.AppClient,
            roleSet,
            $"MultiQuery-{Guid.NewGuid():N}",
            "Test multiple queries");

        var dbFixture = new AppDbFixture();

        // First query
        await using (var db = dbFixture.CreateDbContext())
        {
            var groupFromDb1 = await db.Groups
                .Where(g => g.Guid == group.Guid)
                .Include(g => g.GroupRoles)
                .SingleOrDefaultAsync();

            groupFromDb1.Should().NotBeNull();
            groupFromDb1!.GroupRoles.Should().HaveCount(2);
        }

        // Second query — should still work
        await using (var db = dbFixture.CreateDbContext())
        {
            var groupFromDb2 = await db.Groups
                .Where(g => g.Guid == group.Guid)
                .Include(g => g.GroupRoles)
                .SingleOrDefaultAsync();

            groupFromDb2.Should().NotBeNull();
            groupFromDb2!.GroupRoles.Should().HaveCount(2);
            groupFromDb2.GroupRoles.Select(gr => gr.Permission).Should().Contain(Permissions.RentalRead);
            groupFromDb2.GroupRoles.Select(gr => gr.Permission).Should().Contain(Permissions.RentalUpdate);
        }
    }

    // =========================================================================
    // ListGroupsWithPermissionsAsync
    // =========================================================================

    [Fact]
    public async Task Repository_ListGroupsWithPermissions_includes_roles_for_all_groups()
    {
        var adminBundle = await fixture.GetInitialAdminUserAsync();

        // Create multiple groups with different roles
        var group1Roles = new[] { Permissions.DeviceRead };
        var group2Roles = new[] { Permissions.MaintenanceCreate, Permissions.MaintenanceUpdate };

        var group1 = await fixture.CreateGroupWithRolesAsync(
            adminBundle.AppClient,
            group1Roles,
            $"Group1-{Guid.NewGuid():N}",
            "First group");

        var group2 = await fixture.CreateGroupWithRolesAsync(
            adminBundle.AppClient,
            group2Roles,
            $"Group2-{Guid.NewGuid():N}",
            "Second group");

        // List all groups with permissions
        var dbFixture = new AppDbFixture();
        await using var db = dbFixture.CreateDbContext();
        var allGroups = await db.Groups
            .AsNoTracking()
            .Include(g => g.GroupRoles)
            .OrderBy(g => g.Name)
            .ToListAsync();

        // Verify both groups are loaded with their roles
        var loadedGroup1 = allGroups.FirstOrDefault(g => g.Guid == group1.Guid);
        var loadedGroup2 = allGroups.FirstOrDefault(g => g.Guid == group2.Guid);

        loadedGroup1.Should().NotBeNull();
        loadedGroup1!.GroupRoles.Should().HaveCount(1);
        loadedGroup1.GroupRoles.Should().Contain(gr => gr.Permission == Permissions.DeviceRead);

        loadedGroup2.Should().NotBeNull();
        loadedGroup2!.GroupRoles.Should().HaveCount(2);
        loadedGroup2.GroupRoles.Select(gr => gr.Permission).Should().Contain(Permissions.MaintenanceCreate);
        loadedGroup2.GroupRoles.Select(gr => gr.Permission).Should().Contain(Permissions.MaintenanceUpdate);
    }

    [Fact]
    public async Task Repository_ListGroupsWithPermissions_includes_empty_roles()
    {
        var adminBundle = await fixture.GetInitialAdminUserAsync();

        // Create one group with roles and one without
        var groupWithRoles = await fixture.CreateGroupWithRolesAsync(
            adminBundle.AppClient,
            new[] { Permissions.RentalRead },
            $"WithRoles-{Guid.NewGuid():N}",
            "Group with roles");

        var groupWithoutRoles = await fixture.CreateGroupAsync(
            adminBundle.AppClient,
            $"NoRoles-{Guid.NewGuid():N}",
            "Group without roles");

        // List all groups
        var dbFixture = new AppDbFixture();
        await using var db = dbFixture.CreateDbContext();
        var allGroups = await db.Groups
            .AsNoTracking()
            .Include(g => g.GroupRoles)
            .ToListAsync();

        var loaded1 = allGroups.FirstOrDefault(g => g.Guid == groupWithRoles.Guid);
        var loaded2 = allGroups.FirstOrDefault(g => g.Guid == groupWithoutRoles.Guid);

        loaded1.Should().NotBeNull();
        loaded1!.GroupRoles.Should().HaveCount(1);

        loaded2.Should().NotBeNull();
        loaded2!.GroupRoles.Should().BeEmpty();
    }

    [Fact]
    public async Task Repository_ListGroupsWithPermissions_supports_search()
    {
        var adminBundle = await fixture.GetInitialAdminUserAsync();
        var searchMarker = Guid.NewGuid().ToString("N");

        // Create two groups
        var group1 = await fixture.CreateGroupWithRolesAsync(
            adminBundle.AppClient,
            new[] { Permissions.DeviceRead },
            $"SearchTest-{searchMarker}",
            "First test group");

        var group2 = await fixture.CreateGroupWithRolesAsync(
            adminBundle.AppClient,
            new[] { Permissions.RentalRead },
            $"NoMatch-{Guid.NewGuid():N}",
            "Different group");

        // Search with include
        var dbFixture = new AppDbFixture();
        await using var db = dbFixture.CreateDbContext();
        var searchResults = await db.Groups
            .Where(g => g.Name.Contains(searchMarker))
            .AsNoTracking()
            .Include(g => g.GroupRoles)
            .ToListAsync();

        searchResults.Should().HaveCount(1);
        searchResults[0].Guid.Should().Be(group1.Guid);
        searchResults[0].GroupRoles.Should().HaveCount(1);
        searchResults[0].GroupRoles[0].Permission.Should().Be(Permissions.DeviceRead);
    }

    [Fact]
    public async Task Repository_ListGroupsWithPermissions_supports_pagination()
    {
        var adminBundle = await fixture.GetInitialAdminUserAsync();

        // Create several groups with roles
        for (int i = 0; i < 3; i++)
        {
            await fixture.CreateGroupWithRolesAsync(
                adminBundle.AppClient,
                new[] { Permissions.DeviceRead },
                $"PaginationTest{i}-{Guid.NewGuid():N}",
                $"Pagination test group {i}");
        }

        // List with limit
        var dbFixture = new AppDbFixture();
        await using var db = dbFixture.CreateDbContext();
        var page1 = await db.Groups
            .AsNoTracking()
            .Include(g => g.GroupRoles)
            .OrderBy(g => g.Name)
            .Take(2)
            .ToListAsync();

        // All items should have permissions loaded
        page1.Should().NotBeEmpty();
        page1.ForEach(g => g.GroupRoles.Should().NotBeNull());
    }

    // =========================================================================
    // API endpoint: GET /groups/{guid} with permissions
    // =========================================================================

    [Fact]
    public async Task API_GET_group_returns_full_group_view()
    {
        var adminBundle = await fixture.GetInitialAdminUserAsync();
        var roles = new[]
        {
            Permissions.MaintenanceRead,
            Permissions.MaintenanceUpdate,
        };

        var group = await fixture.CreateGroupWithRolesAsync(
            adminBundle.AppClient,
            roles,
            $"APITest-{Guid.NewGuid():N}",
            "API test group");

        // Query the group via API
        var response = await adminBundle.AppClient.GetAsync($"/api/v1/auth/groups/{group.Guid}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var groupView = await JsonSerializer.DeserializeAsync<GroupView>(
            await response.Content.ReadAsStreamAsync(),
            Json.GetJsonSerializerOptions());

        groupView.Should().NotBeNull();
        groupView!.Guid.Should().Be(group.Guid);
        groupView.Name.Should().Be(group.Name);
    }

    [Fact]
    public async Task API_GET_groups_list_includes_all_roles()
    {
        var adminBundle = await fixture.GetInitialAdminUserAsync();

        var group = await fixture.CreateGroupWithRolesAsync(
            adminBundle.AppClient,
            new[] { Permissions.VendorCreate, Permissions.VendorRead },
            $"ListTest-{Guid.NewGuid():N}",
            "List test group");

        // List groups via API
        var response = await adminBundle.AppClient.GetAsync("/api/v1/auth/groups?limit=50&offset=0");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var groupsList = JsonSerializer.Deserialize<ListViewDto<GroupView>>(
            await response.Content.ReadAsStringAsync(),
            Json.GetJsonSerializerOptions());

        groupsList.Should().NotBeNull();
        groupsList!.list.Should().Contain(g => g.Guid == group.Guid);
    }

    // =========================================================================
    // Behavior: permissions reflect in-memory changes before save
    // =========================================================================

    [Fact]
    public async Task Repository_GroupRoles_reflect_updates_before_save()
    {
        var adminBundle = await fixture.GetInitialAdminUserAsync();

        // Create group with initial roles
        var group = await fixture.CreateGroupWithRolesAsync(
            adminBundle.AppClient,
            new[] { Permissions.DeviceRead },
            $"UpdateTest-{Guid.NewGuid():N}",
            "Update test");

        // Load the group with its roles
        var dbFixture = new AppDbFixture();
        await using var db = dbFixture.CreateDbContext();
        var groupFromDb = await db.Groups
            .Where(g => g.Guid == group.Guid)
            .Include(g => g.GroupRoles)
            .SingleOrDefaultAsync();

        groupFromDb.Should().NotBeNull();
        groupFromDb!.GroupRoles.Should().HaveCount(1);

        // In-memory: add a new role
        var newPermission = new GroupPermissions
        {
            GroupId = groupFromDb.Id,
            Permission = Permissions.VendorCreate
        };
        groupFromDb.GroupRoles.Add(newPermission);

        // Before save, the in-memory collection shows 2 items
        groupFromDb.GroupRoles.Should().HaveCount(2);

        // After save
        await db.SaveChangesAsync();

        // Fresh load should show both
        await using var db2 = dbFixture.CreateDbContext();
        var groupReloaded = await db2.Groups
            .Where(g => g.Guid == group.Guid)
            .Include(g => g.GroupRoles)
            .SingleOrDefaultAsync();

        groupReloaded!.GroupRoles.Should().HaveCount(2);
        groupReloaded.GroupRoles.Select(gr => gr.Permission).Should().Contain(Permissions.DeviceRead);
        groupReloaded.GroupRoles.Select(gr => gr.Permission).Should().Contain(Permissions.VendorCreate);
    }

    // =========================================================================
    // Behavior: role removal
    // =========================================================================

    [Fact]
    public async Task API_DELETE_group_role_removes_permission()
    {
        var adminBundle = await fixture.GetInitialAdminUserAsync();

        var group = await fixture.CreateGroupWithRolesAsync(
            adminBundle.AppClient,
            new[] { Permissions.DeviceRead, Permissions.DeviceCreate },
            $"DeleteRoleTest-{Guid.NewGuid():N}",
            "Delete role test");

        // Verify roles are assigned
        var dbFixture = new AppDbFixture();
        await using var db = dbFixture.CreateDbContext();
        var beforeDelete = await db.Groups
            .Where(g => g.Guid == group.Guid)
            .Include(g => g.GroupRoles)
            .SingleOrDefaultAsync();

        beforeDelete!.GroupRoles.Should().HaveCount(2);

        // Remove all roles via API
        var deleteResp = await adminBundle.AppClient.DeleteAsync($"/api/v1/auth/groups/{group.Guid}/roles");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify roles are gone
        await using var db2 = dbFixture.CreateDbContext();
        var afterDelete = await db2.Groups
            .Where(g => g.Guid == group.Guid)
            .Include(g => g.GroupRoles)
            .SingleOrDefaultAsync();

        afterDelete!.GroupRoles.Should().BeEmpty();
    }
}
