using FluentAssertions;
using LumenForgeServer.IntegrationTests.Collections;
using LumenForgeServer.IntegrationTests.Fixtures;
using LumenForgeServer.Maintenance.Dto.Command;
using LumenForgeServer.Maintenance.Dto.View;
using System.Net;
using System.Net.Http.Json;

namespace LumenForgeServer.IntegrationTests.Maintenance;

/// <summary>
/// Integration tests for <c>api/v1/maintenance/statuses</c> endpoints.
/// </summary>
[Collection(AuthCollection.Name)]
public class MaintenanceStatusEndpointsTests(AuthFixture fixture)
{
    // ── Auth ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GET_statuses_requires_authentication()
    {
        var response = await fixture.GetAnonymousClient().GetAsync("/api/v1/maintenance/statuses");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PUT_status_requires_authentication()
    {
        var response = await fixture.GetAnonymousClient().PutAsJsonAsync("/api/v1/maintenance/statuses",
            new CreateMaintenanceStatusDto { Name = "X" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PUT_status_creates_and_returns_201()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var name = $"Status-{Guid.NewGuid():N}";

        var response = await admin.AppClient.PutAsJsonAsync("/api/v1/maintenance/statuses",
            new CreateMaintenanceStatusDto { Name = name, Description = "Some description" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var view = await MaintenanceTestHelpers.DeserializeAsync<MaintenanceStatusView>(response);
        view.Uuid.Should().NotBe(Guid.Empty);
        view.Name.Should().Be(name);
        view.Description.Should().Be("Some description");
        view.CreatedAt.Should().NotBe(default);
        view.UpdatedAt.Should().NotBe(default);
    }

    [Fact]
    public async Task PUT_status_without_description_creates_successfully()
    {
        var admin = await fixture.GetInitialAdminUserAsync();

        var response = await admin.AppClient.PutAsJsonAsync("/api/v1/maintenance/statuses",
            new CreateMaintenanceStatusDto { Name = $"NoDes-{Guid.NewGuid():N}" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var view = await MaintenanceTestHelpers.DeserializeAsync<MaintenanceStatusView>(response);
        view.Description.Should().BeNull();
    }

    [Fact]
    public async Task PUT_status_with_duplicate_name_returns_409()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var name = $"Dup-{Guid.NewGuid():N}";

        await MaintenanceTestHelpers.CreateStatusAsync(admin, name);

        var response = await admin.AppClient.PutAsJsonAsync("/api/v1/maintenance/statuses",
            new CreateMaintenanceStatusDto { Name = name });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task PUT_status_with_blank_name_returns_400(string name)
    {
        var admin = await fixture.GetInitialAdminUserAsync();

        var response = await admin.AppClient.PutAsJsonAsync("/api/v1/maintenance/statuses",
            new CreateMaintenanceStatusDto { Name = name });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Get single ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GET_status_by_uuid_returns_200()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var created = await MaintenanceTestHelpers.CreateStatusAsync(admin);

        var response = await admin.AppClient.GetAsync($"/api/v1/maintenance/statuses/{created.Uuid}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var view = await MaintenanceTestHelpers.DeserializeAsync<MaintenanceStatusView>(response);
        view.Uuid.Should().Be(created.Uuid);
        view.Name.Should().Be(created.Name);
    }

    [Fact]
    public async Task GET_status_unknown_uuid_returns_404()
    {
        var admin = await fixture.GetInitialAdminUserAsync();

        var response = await admin.AppClient.GetAsync($"/api/v1/maintenance/statuses/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── List ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GET_statuses_returns_created_entries()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var marker = $"ListTest-{Guid.NewGuid():N}";
        await MaintenanceTestHelpers.CreateStatusAsync(admin, marker);

        var response = await admin.AppClient.GetAsync($"/api/v1/maintenance/statuses?search={marker}&limit=10&offset=0");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await MaintenanceTestHelpers.DeserializeAsync<List<MaintenanceStatusView>>(response);
        list.Should().Contain(s => s.Name == marker);
    }

    [Fact]
    public async Task GET_statuses_with_invalid_limit_returns_400()
    {
        var admin = await fixture.GetInitialAdminUserAsync();

        var response = await admin.AppClient.GetAsync("/api/v1/maintenance/statuses?limit=0");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_statuses_search_returns_only_matching()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var uniquePart = Guid.NewGuid().ToString("N")[..8];
        await MaintenanceTestHelpers.CreateStatusAsync(admin, $"Match-{uniquePart}");
        await MaintenanceTestHelpers.CreateStatusAsync(admin, $"Other-{Guid.NewGuid():N}");

        var response = await admin.AppClient.GetAsync($"/api/v1/maintenance/statuses?search={uniquePart}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await MaintenanceTestHelpers.DeserializeAsync<List<MaintenanceStatusView>>(response);
        list.Should().HaveCount(1);
        list[0].Name.Should().Contain(uniquePart);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PATCH_status_updates_name()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var status = await MaintenanceTestHelpers.CreateStatusAsync(admin);
        var newName = $"Renamed-{Guid.NewGuid():N}";

        var response = await admin.AppClient.PatchAsJsonAsync(
            $"/api/v1/maintenance/statuses/{status.Uuid}",
            new UpdateMaintenanceStatusDto { Name = newName });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var view = await MaintenanceTestHelpers.DeserializeAsync<MaintenanceStatusView>(response);
        view.Name.Should().Be(newName);
        view.UpdatedAt.Should().BeGreaterThanOrEqualTo(status.UpdatedAt);
    }

    [Fact]
    public async Task PATCH_status_updates_description_to_null()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var status = await MaintenanceTestHelpers.CreateStatusAsync(admin, description: "Has description");

        var response = await admin.AppClient.PatchAsJsonAsync(
            $"/api/v1/maintenance/statuses/{status.Uuid}",
            new UpdateMaintenanceStatusDto { Description = "" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var view = await MaintenanceTestHelpers.DeserializeAsync<MaintenanceStatusView>(response);
        view.Description.Should().BeNull();
    }

    [Fact]
    public async Task PATCH_status_unknown_uuid_returns_404()
    {
        var admin = await fixture.GetInitialAdminUserAsync();

        var response = await admin.AppClient.PatchAsJsonAsync(
            $"/api/v1/maintenance/statuses/{Guid.NewGuid()}",
            new UpdateMaintenanceStatusDto { Name = "X" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PATCH_status_duplicate_name_returns_409()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var nameA = $"StatusA-{Guid.NewGuid():N}";
        var statusA = await MaintenanceTestHelpers.CreateStatusAsync(admin, nameA);
        var statusB = await MaintenanceTestHelpers.CreateStatusAsync(admin);

        var response = await admin.AppClient.PatchAsJsonAsync(
            $"/api/v1/maintenance/statuses/{statusB.Uuid}",
            new UpdateMaintenanceStatusDto { Name = nameA });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DELETE_status_removes_entry()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var status = await MaintenanceTestHelpers.CreateStatusAsync(admin);

        var deleteResponse = await admin.AppClient.DeleteAsync($"/api/v1/maintenance/statuses/{status.Uuid}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await admin.AppClient.GetAsync($"/api/v1/maintenance/statuses/{status.Uuid}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DELETE_status_unknown_uuid_returns_404()
    {
        var admin = await fixture.GetInitialAdminUserAsync();

        var response = await admin.AppClient.DeleteAsync($"/api/v1/maintenance/statuses/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DELETE_status_in_use_returns_409()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await Inventory.InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await Inventory.InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var status = await MaintenanceTestHelpers.CreateStatusAsync(admin);

        // Create a backlog referencing this status
        await MaintenanceTestHelpers.CreateBacklogAsync(admin, status.Uuid, device.Guid);

        var response = await admin.AppClient.DeleteAsync($"/api/v1/maintenance/statuses/{status.Uuid}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
