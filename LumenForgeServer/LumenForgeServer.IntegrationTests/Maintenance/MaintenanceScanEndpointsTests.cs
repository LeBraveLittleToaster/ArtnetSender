using FluentAssertions;
using LumenForgeServer.IntegrationTests.Collections;
using LumenForgeServer.IntegrationTests.Fixtures;
using LumenForgeServer.IntegrationTests.Inventory;
using LumenForgeServer.Inventory.Domain;
using LumenForgeServer.Inventory.Dto.View;
using LumenForgeServer.Maintenance.Dto.Command;
using LumenForgeServer.Maintenance.Dto.View;
using System.Net;
using System.Net.Http.Json;

namespace LumenForgeServer.IntegrationTests.Maintenance;

[Collection(AuthCollection.Name)]
public class MaintenanceScanEndpointsTests(AuthFixture fixture)
{
    // =========================================================================
    // Authentication
    // =========================================================================

    [Fact]
    public async Task POST_scan_device_for_job_requires_authentication()
    {
        var response = await fixture.GetAnonymousClient()
            .PostAsJsonAsync($"/api/v1/maintenance/jobs/{Guid.NewGuid()}/devices/scan",
                new ScanDeviceDto { DeviceGuid = Guid.NewGuid() });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_scan_device_for_task_requires_authentication()
    {
        var response = await fixture.GetAnonymousClient()
            .PostAsJsonAsync($"/api/v1/maintenance/jobs/{Guid.NewGuid()}/tasks/{Guid.NewGuid()}/devices/scan",
                new ScanDeviceDto { DeviceGuid = Guid.NewGuid() });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // Job scan — happy path
    // =========================================================================

    [Fact]
    public async Task POST_scan_device_for_job_adds_device_to_affected_devices()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var deviceA = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var deviceB = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var job = await MaintenanceTestHelpers.CreateJobAsync(admin, [deviceA.Guid]);

        var response = await admin.AppClient.PostAsJsonAsync(
            $"/api/v1/maintenance/jobs/{job.Guid}/devices/scan",
            new ScanDeviceDto { DeviceGuid = deviceB.Guid });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await MaintenanceTestHelpers.DeserializeAsync<MaintenanceJobView>(response);
        updated.AffectedDeviceGuids.Should().Contain(deviceA.Guid);
        updated.AffectedDeviceGuids.Should().Contain(deviceB.Guid);
    }

    [Fact]
    public async Task POST_scan_device_for_job_creates_maintenance_binding_for_scanned_device()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var seedDevice = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var scannedDevice = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var job = await MaintenanceTestHelpers.CreateJobAsync(admin, [seedDevice.Guid]);

        _ = await admin.AppClient.PostAsJsonAsync(
            $"/api/v1/maintenance/jobs/{job.Guid}/devices/scan",
            new ScanDeviceDto { DeviceGuid = scannedDevice.Guid });

        var deviceResponse = await admin.AppClient.GetAsync(
            $"/api/v1/inventory/devices/{scannedDevice.Guid}");
        var deviceView = await InventoryTestHelpers.DeserializeResponseAsync<DeviceView>(deviceResponse);

        deviceView.StockBindings.Should().Contain(sb => sb.BindingType == BindingType.MAINTENANCE);
    }

    [Fact]
    public async Task POST_scan_device_for_job_is_idempotent()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var job = await MaintenanceTestHelpers.CreateJobAsync(admin, [device.Guid]);

        // Scan the same device that is already on the job twice
        var r1 = await admin.AppClient.PostAsJsonAsync(
            $"/api/v1/maintenance/jobs/{job.Guid}/devices/scan",
            new ScanDeviceDto { DeviceGuid = device.Guid });
        var r2 = await admin.AppClient.PostAsJsonAsync(
            $"/api/v1/maintenance/jobs/{job.Guid}/devices/scan",
            new ScanDeviceDto { DeviceGuid = device.Guid });

        r1.StatusCode.Should().Be(HttpStatusCode.OK);
        r2.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await MaintenanceTestHelpers.DeserializeAsync<MaintenanceJobView>(r2);
        updated.AffectedDeviceGuids.Count(g => g == device.Guid).Should().Be(1);
    }

    // =========================================================================
    // Job scan — error cases
    // =========================================================================

    [Fact]
    public async Task POST_scan_device_for_job_with_unknown_job_returns_not_found()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);

        var response = await admin.AppClient.PostAsJsonAsync(
            $"/api/v1/maintenance/jobs/{Guid.NewGuid()}/devices/scan",
            new ScanDeviceDto { DeviceGuid = device.Guid });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_scan_device_for_job_with_unknown_device_returns_not_found()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var job = await MaintenanceTestHelpers.CreateJobAsync(admin, [device.Guid]);

        var response = await admin.AppClient.PostAsJsonAsync(
            $"/api/v1/maintenance/jobs/{job.Guid}/devices/scan",
            new ScanDeviceDto { DeviceGuid = Guid.NewGuid() });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Task scan — happy path
    // =========================================================================

    [Fact]
    public async Task POST_scan_device_for_task_adds_device_to_task_affected_devices()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var deviceA = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var deviceB = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var job = await MaintenanceTestHelpers.CreateJobAsync(admin, [deviceA.Guid]);
        var task = await MaintenanceTestHelpers.CreateTaskAsync(admin, job.Guid);

        var response = await admin.AppClient.PostAsJsonAsync(
            $"/api/v1/maintenance/jobs/{job.Guid}/tasks/{task.Guid}/devices/scan",
            new ScanDeviceDto { DeviceGuid = deviceB.Guid });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await MaintenanceTestHelpers.DeserializeAsync<MaintenanceTaskView>(response);
        updated.AffectedDeviceGuids.Should().Contain(deviceB.Guid);
    }

    [Fact]
    public async Task POST_scan_device_for_task_does_not_affect_other_tasks()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var newDevice = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var job = await MaintenanceTestHelpers.CreateJobAsync(admin, [device.Guid]);
        var taskA = await MaintenanceTestHelpers.CreateTaskAsync(admin, job.Guid);
        var taskB = await MaintenanceTestHelpers.CreateTaskAsync(admin, job.Guid);

        _ = await admin.AppClient.PostAsJsonAsync(
            $"/api/v1/maintenance/jobs/{job.Guid}/tasks/{taskA.Guid}/devices/scan",
            new ScanDeviceDto { DeviceGuid = newDevice.Guid });

        // taskB must be unaffected
        var taskBResponse = await admin.AppClient.GetAsync(
            $"/api/v1/maintenance/jobs/{job.Guid}/tasks?include=Devices");
        taskBResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task POST_scan_device_for_task_is_idempotent()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var job = await MaintenanceTestHelpers.CreateJobAsync(admin, [device.Guid]);
        var task = await MaintenanceTestHelpers.CreateTaskAsync(admin, job.Guid);

        var r1 = await admin.AppClient.PostAsJsonAsync(
            $"/api/v1/maintenance/jobs/{job.Guid}/tasks/{task.Guid}/devices/scan",
            new ScanDeviceDto { DeviceGuid = device.Guid });
        var r2 = await admin.AppClient.PostAsJsonAsync(
            $"/api/v1/maintenance/jobs/{job.Guid}/tasks/{task.Guid}/devices/scan",
            new ScanDeviceDto { DeviceGuid = device.Guid });

        r1.StatusCode.Should().Be(HttpStatusCode.OK);
        r2.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await MaintenanceTestHelpers.DeserializeAsync<MaintenanceTaskView>(r2);
        updated.AffectedDeviceGuids.Count(g => g == device.Guid).Should().Be(1);
    }

    // =========================================================================
    // Task scan — error cases
    // =========================================================================

    [Fact]
    public async Task POST_scan_device_for_task_with_unknown_task_returns_not_found()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var job = await MaintenanceTestHelpers.CreateJobAsync(admin, [device.Guid]);

        var response = await admin.AppClient.PostAsJsonAsync(
            $"/api/v1/maintenance/jobs/{job.Guid}/tasks/{Guid.NewGuid()}/devices/scan",
            new ScanDeviceDto { DeviceGuid = device.Guid });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_scan_device_for_task_with_unknown_device_returns_not_found()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var job = await MaintenanceTestHelpers.CreateJobAsync(admin, [device.Guid]);
        var task = await MaintenanceTestHelpers.CreateTaskAsync(admin, job.Guid);

        var response = await admin.AppClient.PostAsJsonAsync(
            $"/api/v1/maintenance/jobs/{job.Guid}/tasks/{task.Guid}/devices/scan",
            new ScanDeviceDto { DeviceGuid = Guid.NewGuid() });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_scan_device_for_task_with_mismatched_job_returns_not_found()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var deviceA = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var deviceB = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var jobA = await MaintenanceTestHelpers.CreateJobAsync(admin, [deviceA.Guid]);
        var jobB = await MaintenanceTestHelpers.CreateJobAsync(admin, [deviceB.Guid]);
        var taskUnderJobA = await MaintenanceTestHelpers.CreateTaskAsync(admin, jobA.Guid);

        // Pass jobB's GUID in the route but the task belongs to jobA
        var response = await admin.AppClient.PostAsJsonAsync(
            $"/api/v1/maintenance/jobs/{jobB.Guid}/tasks/{taskUnderJobA.Guid}/devices/scan",
            new ScanDeviceDto { DeviceGuid = deviceB.Guid });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
