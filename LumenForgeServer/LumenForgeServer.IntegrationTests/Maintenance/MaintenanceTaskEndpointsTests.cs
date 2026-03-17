using FluentAssertions;
using LumenForgeServer.Auth.Dto.Views;
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
public class MaintenanceTaskEndpointsTests(AuthFixture fixture)
{
    [Fact]
    public async Task POST_task_creates_for_job()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var job = await MaintenanceTestHelpers.CreateJobAsync(admin, [device.Guid]);

        var response = await admin.AppClient.PostAsJsonAsync($"/api/v1/maintenance/jobs/{job.Guid}/tasks", new CreateMaintenanceTaskDto
        {
            Description = "Replace fan",
            Status = MaintenanceStatus.UnderInvestigation,
            AssignedToUserKcId = admin.GetKcUserId(),
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var task = await MaintenanceTestHelpers.DeserializeAsync<MaintenanceTaskView>(response);
        task.Description.Should().Be("Replace fan");
        task.Status.Should().Be(MaintenanceStatus.UnderInvestigation);
        task.AssignedToUserKcId.Should().Be(admin.GetKcUserId());
    }

    [Fact]
    public async Task PATCH_task_resolved_resolves_job_and_closes_maintenance_binding()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var job = await MaintenanceTestHelpers.CreateJobAsync(admin, [device.Guid]);
        var task = await MaintenanceTestHelpers.CreateTaskAsync(admin, job.Guid);

        var response = await admin.AppClient.PatchAsJsonAsync($"/api/v1/maintenance/jobs/{job.Guid}/tasks/{task.Guid}", new UpdateMaintenanceTaskDto
        {
            Status = MaintenanceStatus.Resolved,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var jobResponse = await admin.AppClient.GetAsync($"/api/v1/maintenance/jobs/{job.Guid}");
        var updatedJob = await MaintenanceTestHelpers.DeserializeAsync<MaintenanceJobView>(jobResponse);
        updatedJob.Status.Should().Be(MaintenanceStatus.Resolved);
        updatedJob.ResolvedAt.Should().NotBeNull();

        var deviceResponse = await admin.AppClient.GetAsync($"/api/v1/inventory/devices/{device.Guid}");
        var deviceView = await InventoryTestHelpers.DeserializeResponseAsync<DeviceView>(deviceResponse);
        deviceView.StockBindings
            .Where(sb => sb.BindingType == BindingType.MAINTENANCE)
            .Should().OnlyContain(sb => sb.End <= updatedJob.ResolvedAt!.Value);
    }

    [Fact]
    public async Task POST_task_log_adds_log_and_can_change_status()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var job = await MaintenanceTestHelpers.CreateJobAsync(admin, [device.Guid]);
        var task = await MaintenanceTestHelpers.CreateTaskAsync(admin, job.Guid);

        var response = await admin.AppClient.PostAsJsonAsync($"/api/v1/maintenance/jobs/{job.Guid}/tasks/{task.Guid}/logs", new CreateMaintenanceLogEntryDto
        {
            Name = "Inspection update",
            Description = "Found spare part and completed replacement.",
            StatusAfter = MaintenanceStatus.Resolved,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var log = await MaintenanceTestHelpers.DeserializeAsync<MaintenanceLogEntryView>(response);
        log.StatusBefore.Should().Be(MaintenanceStatus.Reported);
        log.StatusAfter.Should().Be(MaintenanceStatus.Resolved);

        var logsResponse = await admin.AppClient.GetAsync($"/api/v1/maintenance/jobs/{job.Guid}/tasks/{task.Guid}/logs");
        logsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var logs = await MaintenanceTestHelpers.DeserializeAsync<ListViewDto<MaintenanceLogEntryView>>(logsResponse);
        logs.list.Should().Contain(l => l.Guid == log.Guid);
    }

    [Fact]
    public async Task PATCH_task_with_mismatched_job_returns_not_found()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var deviceA = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var deviceB = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var jobA = await MaintenanceTestHelpers.CreateJobAsync(admin, [deviceA.Guid]);
        var jobB = await MaintenanceTestHelpers.CreateJobAsync(admin, [deviceB.Guid]);
        var task = await MaintenanceTestHelpers.CreateTaskAsync(admin, jobA.Guid);

        var response = await admin.AppClient.PatchAsJsonAsync($"/api/v1/maintenance/jobs/{jobB.Guid}/tasks/{task.Guid}", new UpdateMaintenanceTaskDto
        {
            Status = MaintenanceStatus.Resolved,
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DELETE_task_removes_it_from_job()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var job = await MaintenanceTestHelpers.CreateJobAsync(admin, [device.Guid]);
        var task = await MaintenanceTestHelpers.CreateTaskAsync(admin, job.Guid);

        var deleteResponse = await admin.AppClient.DeleteAsync($"/api/v1/maintenance/jobs/{job.Guid}/tasks/{task.Guid}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listResponse = await admin.AppClient.GetAsync($"/api/v1/maintenance/jobs/{job.Guid}/tasks");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = JsonSerializer.Deserialize<JsonElement>(await listResponse.Content.ReadAsStringAsync());
        var tasks = body.GetProperty("list").Deserialize<List<MaintenanceTaskView>>(Json.GetJsonSerializerOptions())!;
        tasks.Should().NotContain(t => t.Guid == task.Guid);
    }

    [Fact]
    public async Task POST_task_log_without_status_change_keeps_status()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var job = await MaintenanceTestHelpers.CreateJobAsync(admin, [device.Guid]);
        var task = await MaintenanceTestHelpers.CreateTaskAsync(admin, job.Guid);

        var response = await admin.AppClient.PostAsJsonAsync($"/api/v1/maintenance/jobs/{job.Guid}/tasks/{task.Guid}/logs", new CreateMaintenanceLogEntryDto
        {
            Name = "Progress",
            Description = "Investigating further.",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var tasksResponse = await admin.AppClient.GetAsync($"/api/v1/maintenance/jobs/{job.Guid}/tasks");
        var body = JsonSerializer.Deserialize<JsonElement>(await tasksResponse.Content.ReadAsStringAsync());
        var tasks = body.GetProperty("list").Deserialize<List<MaintenanceTaskView>>(Json.GetJsonSerializerOptions())!;
        tasks.Single(t => t.Guid == task.Guid).Status.Should().Be(MaintenanceStatus.Reported);
    }

    [Fact]
    public async Task GET_tasks_supports_pagination()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var job = await MaintenanceTestHelpers.CreateJobAsync(admin, [device.Guid]);

        _ = await MaintenanceTestHelpers.CreateTaskAsync(admin, job.Guid, "Task-A");
        _ = await MaintenanceTestHelpers.CreateTaskAsync(admin, job.Guid, "Task-B");
        _ = await MaintenanceTestHelpers.CreateTaskAsync(admin, job.Guid, "Task-C");

        var response = await admin.AppClient.GetAsync($"/api/v1/maintenance/jobs/{job.Guid}/tasks?limit=2&offset=0");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
        var total = body.GetProperty("total").GetInt64();
        var tasks = body.GetProperty("list").Deserialize<List<MaintenanceTaskView>>(Json.GetJsonSerializerOptions())!;

        total.Should().BeGreaterThanOrEqualTo(3);
        tasks.Should().HaveCount(2);
    }

    [Fact]
    public async Task GET_tasks_include_logs_and_devices_returns_enriched_tasks()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var job = await MaintenanceTestHelpers.CreateJobAsync(admin, [device.Guid]);

        var createTaskResponse = await admin.AppClient.PostAsJsonAsync($"/api/v1/maintenance/jobs/{job.Guid}/tasks", new CreateMaintenanceTaskDto
        {
            Description = "Task with includes",
            AffectedDeviceGuids = [device.Guid],
        });
        createTaskResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var task = await MaintenanceTestHelpers.DeserializeAsync<MaintenanceTaskView>(createTaskResponse);

        var logResponse = await admin.AppClient.PostAsJsonAsync($"/api/v1/maintenance/jobs/{job.Guid}/tasks/{task.Guid}/logs", new CreateMaintenanceLogEntryDto
        {
            Name = "IncludeLog",
            Description = "Entry for include coverage"
        });
        logResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await admin.AppClient.GetAsync($"/api/v1/maintenance/jobs/{job.Guid}/tasks?include=logs,devices&limit=20&offset=0");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
        var tasks = body.GetProperty("list").Deserialize<List<MaintenanceTaskView>>(Json.GetJsonSerializerOptions())!;

        var includedTask = tasks.Single(t => t.Guid == task.Guid);
        includedTask.AffectedDeviceGuids.Should().Contain(device.Guid);
        includedTask.Log.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GET_tasks_invalid_limit_returns_bad_request()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var job = await MaintenanceTestHelpers.CreateJobAsync(admin, [device.Guid]);

        var response = await admin.AppClient.GetAsync($"/api/v1/maintenance/jobs/{job.Guid}/tasks?limit=0&offset=0");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_tasks_with_invalid_include_returns_bad_request()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var job = await MaintenanceTestHelpers.CreateJobAsync(admin, [device.Guid]);

        var response = await admin.AppClient.GetAsync($"/api/v1/maintenance/jobs/{job.Guid}/tasks?include=badFlag");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_task_with_blank_description_returns_bad_request()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var job = await MaintenanceTestHelpers.CreateJobAsync(admin, [device.Guid]);

        var response = await admin.AppClient.PostAsJsonAsync($"/api/v1/maintenance/jobs/{job.Guid}/tasks", new CreateMaintenanceTaskDto
        {
            Description = "   "
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_task_log_with_blank_name_returns_bad_request()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var job = await MaintenanceTestHelpers.CreateJobAsync(admin, [device.Guid]);
        var task = await MaintenanceTestHelpers.CreateTaskAsync(admin, job.Guid);

        var response = await admin.AppClient.PostAsJsonAsync($"/api/v1/maintenance/jobs/{job.Guid}/tasks/{task.Guid}/logs", new CreateMaintenanceLogEntryDto
        {
            Name = " ",
            Description = "desc"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
