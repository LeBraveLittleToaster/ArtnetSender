using FluentAssertions;
using LumenForgeServer.Common;
using LumenForgeServer.IntegrationTests.Collections;
using LumenForgeServer.IntegrationTests.Fixtures;
using LumenForgeServer.IntegrationTests.Inventory;
using LumenForgeServer.Inventory.Dto.View;
using LumenForgeServer.Maintenance.Domain;
using LumenForgeServer.Maintenance.Dto.Command;
using LumenForgeServer.Maintenance.Dto.View;
using BindingType = LumenForgeServer.Inventory.Domain.BindingType;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace LumenForgeServer.IntegrationTests.Maintenance;

[Collection(AuthCollection.Name)]
public class MaintenanceJobEndpointsTests(AuthFixture fixture)
{
    [Fact]
    public async Task GET_jobs_requires_authentication()
    {
        var response = await fixture.GetAnonymousClient().GetAsync("/api/v1/maintenance/jobs");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PUT_job_creates_and_binds_devices_with_maintenance_stockbinding()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);

        var response = await admin.AppClient.PutAsJsonAsync("/api/v1/maintenance/jobs", new CreateMaintenanceJobDto
        {
            Name = "Battery replacement",
            Description = "Battery dropped under threshold.",
            DeviceGuids = [device.Guid]
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await MaintenanceTestHelpers.DeserializeAsync<MaintenanceJobView>(response);
        created.Guid.Should().NotBe(Guid.Empty);
        created.Status.Should().Be(MaintenanceStatus.Reported);
        created.AffectedDeviceGuids.Should().Contain(device.Guid);

        var deviceResponse = await admin.AppClient.GetAsync($"/api/v1/inventory/devices/{device.Guid}");
        var deviceView = await InventoryTestHelpers.DeserializeResponseAsync<DeviceView>(deviceResponse);
        deviceView.StockBindings.Should().Contain(sb => sb.BindingType == BindingType.MAINTENANCE);
    }

    [Fact]
    public async Task PUT_job_without_devices_returns_bad_request()
    {
        var admin = await fixture.GetInitialAdminUserAsync();

        var response = await admin.AppClient.PutAsJsonAsync("/api/v1/maintenance/jobs", new CreateMaintenanceJobDto
        {
            Name = "Invalid",
            Description = "No devices",
            DeviceGuids = []
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PUT_job_with_unknown_device_returns_not_found()
    {
        var admin = await fixture.GetInitialAdminUserAsync();

        var response = await admin.AppClient.PutAsJsonAsync("/api/v1/maintenance/jobs", new CreateMaintenanceJobDto
        {
            Name = "Unknown device",
            Description = "No such device",
            DeviceGuids = [Guid.NewGuid()]
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_job_unknown_uuid_returns_not_found()
    {
        var admin = await fixture.GetInitialAdminUserAsync();

        var response = await admin.AppClient.GetAsync($"/api/v1/maintenance/jobs/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PATCH_job_updates_name_description_and_status()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var job = await MaintenanceTestHelpers.CreateJobAsync(admin, [device.Guid]);

        var response = await admin.AppClient.PatchAsJsonAsync($"/api/v1/maintenance/jobs/{job.Guid}", new UpdateMaintenanceJobDto
        {
            Name = "Updated Name",
            Description = "Updated Description",
            Status = MaintenanceStatus.UnderInvestigation
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await MaintenanceTestHelpers.DeserializeAsync<MaintenanceJobView>(response);
        updated.Name.Should().Be("Updated Name");
        updated.Description.Should().Be("Updated Description");
        updated.Status.Should().Be(MaintenanceStatus.UnderInvestigation);
    }

    [Fact]
    public async Task GET_jobs_supports_search_and_paging()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var marker = $"JobSearch-{Guid.NewGuid():N}";
        _ = await MaintenanceTestHelpers.CreateJobAsync(admin, [device.Guid], marker);

        var response = await admin.AppClient.GetAsync($"/api/v1/maintenance/jobs?search={marker}&limit=10&offset=0");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
        var total = body.GetProperty("total").GetInt64();
        var list = body.GetProperty("list").Deserialize<List<MaintenanceJobView>>(Json.GetJsonSerializerOptions())!;
        total.Should().BeGreaterThan(0);
        list.Should().Contain(j => j.Name == marker);
    }

    [Fact]
    public async Task GET_jobs_pagination_returns_limited_page()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);

        _ = await MaintenanceTestHelpers.CreateJobAsync(admin, [device.Guid], $"PageA-{Guid.NewGuid():N}");
        _ = await MaintenanceTestHelpers.CreateJobAsync(admin, [device.Guid], $"PageB-{Guid.NewGuid():N}");
        _ = await MaintenanceTestHelpers.CreateJobAsync(admin, [device.Guid], $"PageC-{Guid.NewGuid():N}");

        var response = await admin.AppClient.GetAsync("/api/v1/maintenance/jobs?limit=2&offset=0");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
        var total = body.GetProperty("total").GetInt64();
        var list = body.GetProperty("list").Deserialize<List<MaintenanceJobView>>(Json.GetJsonSerializerOptions())!;

        total.Should().BeGreaterThanOrEqualTo(3);
        list.Should().HaveCount(2);
    }

    [Fact]
    public async Task GET_job_include_tasks_and_logs_returns_nested_data()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var job = await MaintenanceTestHelpers.CreateJobAsync(admin, [device.Guid]);
        var task = await MaintenanceTestHelpers.CreateTaskAsync(admin, job.Guid, "Include-test-task");

        var logResponse = await admin.AppClient.PostAsJsonAsync($"/api/v1/maintenance/jobs/{job.Guid}/tasks/{task.Guid}/logs", new CreateMaintenanceLogEntryDto
        {
            Name = "Log1",
            Description = "Log entry for include test"
        });
        logResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await admin.AppClient.GetAsync($"/api/v1/maintenance/jobs/{job.Guid}?include=tasks,logs,devices");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var loaded = await MaintenanceTestHelpers.DeserializeAsync<MaintenanceJobView>(response);
        loaded.AffectedDeviceGuids.Should().Contain(device.Guid);
        loaded.Tasks.Should().Contain(t => t.Guid == task.Guid);
        loaded.Tasks.Single(t => t.Guid == task.Guid).Log.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GET_jobs_include_devices_returns_affected_devices()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var job = await MaintenanceTestHelpers.CreateJobAsync(admin, [device.Guid]);

        var response = await admin.AppClient.GetAsync("/api/v1/maintenance/jobs?include=devices&limit=50&offset=0");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
        var list = body.GetProperty("list").Deserialize<List<MaintenanceJobView>>(Json.GetJsonSerializerOptions())!;
        list.Should().Contain(j => j.Guid == job.Guid && j.AffectedDeviceGuids.Contains(device.Guid));
    }

    [Fact]
    public async Task DELETE_job_removes_record()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var job = await MaintenanceTestHelpers.CreateJobAsync(admin, [device.Guid]);

        var deleteResponse = await admin.AppClient.DeleteAsync($"/api/v1/maintenance/jobs/{job.Guid}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await admin.AppClient.GetAsync($"/api/v1/maintenance/jobs/{job.Guid}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_jobs_invalid_limit_returns_bad_request()
    {
        var admin = await fixture.GetInitialAdminUserAsync();

        var response = await admin.AppClient.GetAsync("/api/v1/maintenance/jobs?limit=0&offset=0");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_jobs_with_invalid_include_returns_bad_request()
    {
        var admin = await fixture.GetInitialAdminUserAsync();

        var response = await admin.AppClient.GetAsync("/api/v1/maintenance/jobs?include=invalidIncludeValue");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_job_with_invalid_include_returns_bad_request()
    {
        var admin = await fixture.GetInitialAdminUserAsync();

        var response = await admin.AppClient.GetAsync($"/api/v1/maintenance/jobs/{Guid.NewGuid()}?include=oops");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PATCH_job_with_blank_name_returns_bad_request()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var job = await MaintenanceTestHelpers.CreateJobAsync(admin, [device.Guid]);

        var response = await admin.AppClient.PatchAsJsonAsync($"/api/v1/maintenance/jobs/{job.Guid}", new UpdateMaintenanceJobDto
        {
            Name = "   "
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
