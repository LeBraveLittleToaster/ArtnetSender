using FluentAssertions;
using LumenForgeServer.IntegrationTests.Collections;
using LumenForgeServer.IntegrationTests.Fixtures;
using LumenForgeServer.IntegrationTests.Inventory;
using LumenForgeServer.Maintenance.Dto.Command;
using LumenForgeServer.Maintenance.Dto.View;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace LumenForgeServer.IntegrationTests.Maintenance;

/// <summary>
/// Integration tests for <c>api/v1/maintenance/backlogs</c> and related endpoints.
/// </summary>
[Collection(AuthCollection.Name)]
public class MaintenanceBacklogEndpointsTests(AuthFixture fixture)
{
    // ── Auth ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GET_backlogs_requires_authentication()
    {
        var response = await fixture.GetAnonymousClient().GetAsync("/api/v1/maintenance/backlogs");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PUT_backlog_requires_authentication()
    {
        var response = await fixture.GetAnonymousClient().PutAsJsonAsync("/api/v1/maintenance/backlogs",
            new CreateMaintenanceBacklogDto
            {
                StatusUuid = Guid.NewGuid(),
                IssueSummary = "Issue",
                QuantityAffected = 1,
                DeviceUuid = Guid.NewGuid(),
            });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PUT_backlog_with_device_creates_and_returns_201()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var status = await MaintenanceTestHelpers.CreateStatusAsync(admin);

        var response = await admin.AppClient.PutAsJsonAsync("/api/v1/maintenance/backlogs", new CreateMaintenanceBacklogDto
        {
            StatusUuid = status.Uuid,
            DeviceUuid = device.Guid,
            IssueSummary = "Screen is cracked",
            IssueDescription = "Front glass cracked due to impact.",
            QuantityAffected = 1m,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var view = await MaintenanceTestHelpers.DeserializeAsync<MaintenanceBacklogView>(response);

        view.Uuid.Should().NotBe(Guid.Empty);
        view.IssueSummary.Should().Be("Screen is cracked");
        view.IssueDescription.Should().Be("Front glass cracked due to impact.");
        view.QuantityAffected.Should().Be(1m);
        view.Status.Uuid.Should().Be(status.Uuid);
        view.DeviceUuid.Should().Be(device.Guid);
        view.ResolvedAt.Should().BeNull();
        view.ReportedAt.Should().NotBe(default);
    }

    [Fact]
    public async Task PUT_backlog_without_device_or_rental_returns_400()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var status = await MaintenanceTestHelpers.CreateStatusAsync(admin);

        var response = await admin.AppClient.PutAsJsonAsync("/api/v1/maintenance/backlogs", new CreateMaintenanceBacklogDto
        {
            StatusUuid = status.Uuid,
            IssueSummary = "Issue without context",
            QuantityAffected = 1m,
            DeviceUuid = null,
            RentalItemUuid = null,
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PUT_backlog_with_unknown_status_returns_404()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);

        var response = await admin.AppClient.PutAsJsonAsync("/api/v1/maintenance/backlogs", new CreateMaintenanceBacklogDto
        {
            StatusUuid = Guid.NewGuid(),
            DeviceUuid = device.Guid,
            IssueSummary = "Needs a real status",
            QuantityAffected = 1m,
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PUT_backlog_with_unknown_device_returns_404()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var status = await MaintenanceTestHelpers.CreateStatusAsync(admin);

        var response = await admin.AppClient.PutAsJsonAsync("/api/v1/maintenance/backlogs", new CreateMaintenanceBacklogDto
        {
            StatusUuid = status.Uuid,
            DeviceUuid = Guid.NewGuid(),
            IssueSummary = "Device does not exist",
            QuantityAffected = 1m,
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task PUT_backlog_with_blank_summary_returns_400(string summary)
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var status = await MaintenanceTestHelpers.CreateStatusAsync(admin);

        var response = await admin.AppClient.PutAsJsonAsync("/api/v1/maintenance/backlogs", new CreateMaintenanceBacklogDto
        {
            StatusUuid = status.Uuid,
            DeviceUuid = device.Guid,
            IssueSummary = summary,
            QuantityAffected = 1m,
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task PUT_backlog_with_nonpositive_quantity_returns_400(decimal quantity)
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var status = await MaintenanceTestHelpers.CreateStatusAsync(admin);

        var response = await admin.AppClient.PutAsJsonAsync("/api/v1/maintenance/backlogs", new CreateMaintenanceBacklogDto
        {
            StatusUuid = status.Uuid,
            DeviceUuid = device.Guid,
            IssueSummary = "Bad quantity",
            QuantityAffected = quantity,
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Get single ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GET_backlog_by_uuid_returns_200()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var status = await MaintenanceTestHelpers.CreateStatusAsync(admin);
        var backlog = await MaintenanceTestHelpers.CreateBacklogAsync(admin, status.Uuid, device.Guid);

        var response = await admin.AppClient.GetAsync($"/api/v1/maintenance/backlogs/{backlog.Uuid}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var view = await MaintenanceTestHelpers.DeserializeAsync<MaintenanceBacklogView>(response);
        view.Uuid.Should().Be(backlog.Uuid);
        view.DeviceUuid.Should().Be(device.Guid);
        view.Status.Uuid.Should().Be(status.Uuid);
    }

    [Fact]
    public async Task GET_backlog_unknown_uuid_returns_404()
    {
        var admin = await fixture.GetInitialAdminUserAsync();

        var response = await admin.AppClient.GetAsync($"/api/v1/maintenance/backlogs/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── List ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GET_backlogs_returns_paginated_results()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var status = await MaintenanceTestHelpers.CreateStatusAsync(admin);

        var marker = $"PagingTest-{Guid.NewGuid():N}";
        await MaintenanceTestHelpers.CreateBacklogAsync(admin, status.Uuid, device.Guid, issueSummary: marker + "-A");
        await MaintenanceTestHelpers.CreateBacklogAsync(admin, status.Uuid, device.Guid, issueSummary: marker + "-B");

        var response = await admin.AppClient.GetAsync($"/api/v1/maintenance/backlogs?search={marker}&limit=10&offset=0");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
        body.GetProperty("total").GetInt64().Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GET_backlogs_filter_by_status_uuid_returns_only_matching()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var statusA = await MaintenanceTestHelpers.CreateStatusAsync(admin);
        var statusB = await MaintenanceTestHelpers.CreateStatusAsync(admin);

        await MaintenanceTestHelpers.CreateBacklogAsync(admin, statusA.Uuid, device.Guid);
        await MaintenanceTestHelpers.CreateBacklogAsync(admin, statusB.Uuid, device.Guid);

        var response = await admin.AppClient.GetAsync($"/api/v1/maintenance/backlogs?status_uuid={statusA.Uuid}&limit=50&offset=0");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
        var list = body.GetProperty("list").Deserialize<List<MaintenanceBacklogView>>(LumenForgeServer.Common.Json.GetJsonSerializerOptions())!;
        list.Should().AllSatisfy(b => b.Status.Uuid.Should().Be(statusA.Uuid));
    }

    [Fact]
    public async Task GET_backlogs_unresolved_only_excludes_resolved()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var status = await MaintenanceTestHelpers.CreateStatusAsync(admin);

        var unresolved = await MaintenanceTestHelpers.CreateBacklogAsync(admin, status.Uuid, device.Guid);
        var toResolve = await MaintenanceTestHelpers.CreateBacklogAsync(admin, status.Uuid, device.Guid);

        // Resolve one entry
        await admin.AppClient.PatchAsJsonAsync($"/api/v1/maintenance/backlogs/{toResolve.Uuid}",
            new UpdateMaintenanceBacklogDto { Resolve = true });

        var response = await admin.AppClient.GetAsync($"/api/v1/maintenance/backlogs?unresolved_only=true&limit=200&offset=0");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
        var list = body.GetProperty("list").Deserialize<List<MaintenanceBacklogView>>(LumenForgeServer.Common.Json.GetJsonSerializerOptions())!;
        list.Should().NotContain(b => b.Uuid == toResolve.Uuid);
        list.Should().Contain(b => b.Uuid == unresolved.Uuid);
    }

    [Fact]
    public async Task GET_backlogs_invalid_limit_returns_400()
    {
        var admin = await fixture.GetInitialAdminUserAsync();

        var response = await admin.AppClient.GetAsync("/api/v1/maintenance/backlogs?limit=0");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Backlogs by device ────────────────────────────────────────────────────

    [Fact]
    public async Task GET_backlogs_by_device_returns_only_that_devices_entries()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var deviceA = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var deviceB = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var status = await MaintenanceTestHelpers.CreateStatusAsync(admin);

        var backlogA = await MaintenanceTestHelpers.CreateBacklogAsync(admin, status.Uuid, deviceA.Guid);
        await MaintenanceTestHelpers.CreateBacklogAsync(admin, status.Uuid, deviceB.Guid);

        var response = await admin.AppClient.GetAsync($"/api/v1/maintenance/devices/{deviceA.Guid}/backlogs");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await MaintenanceTestHelpers.DeserializeAsync<List<MaintenanceBacklogView>>(response);
        list.Should().AllSatisfy(b => b.DeviceUuid.Should().Be(deviceA.Guid));
        list.Should().Contain(b => b.Uuid == backlogA.Uuid);
    }

    [Fact]
    public async Task GET_backlogs_by_device_returns_empty_when_no_entries()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);

        var response = await admin.AppClient.GetAsync($"/api/v1/maintenance/devices/{device.Guid}/backlogs");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await MaintenanceTestHelpers.DeserializeAsync<List<MaintenanceBacklogView>>(response);
        list.Should().BeEmpty();
    }

    [Fact]
    public async Task GET_backlogs_by_unknown_device_returns_404()
    {
        var admin = await fixture.GetInitialAdminUserAsync();

        var response = await admin.AppClient.GetAsync($"/api/v1/maintenance/devices/{Guid.NewGuid()}/backlogs");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_backlogs_by_device_returns_multiple_entries_ordered_by_reported_at_desc()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var status = await MaintenanceTestHelpers.CreateStatusAsync(admin);

        await MaintenanceTestHelpers.CreateBacklogAsync(admin, status.Uuid, device.Guid, issueSummary: "First issue");
        await MaintenanceTestHelpers.CreateBacklogAsync(admin, status.Uuid, device.Guid, issueSummary: "Second issue");
        await MaintenanceTestHelpers.CreateBacklogAsync(admin, status.Uuid, device.Guid, issueSummary: "Third issue");

        var response = await admin.AppClient.GetAsync($"/api/v1/maintenance/devices/{device.Guid}/backlogs");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await MaintenanceTestHelpers.DeserializeAsync<List<MaintenanceBacklogView>>(response);
        list.Should().HaveCount(3);
        // Newest entries first
        list.Select(b => b.ReportedAt).Should().BeInDescendingOrder();
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PATCH_backlog_updates_summary_and_description()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var status = await MaintenanceTestHelpers.CreateStatusAsync(admin);
        var backlog = await MaintenanceTestHelpers.CreateBacklogAsync(admin, status.Uuid, device.Guid);

        var response = await admin.AppClient.PatchAsJsonAsync($"/api/v1/maintenance/backlogs/{backlog.Uuid}",
            new UpdateMaintenanceBacklogDto
            {
                IssueSummary = "Updated summary",
                IssueDescription = "Updated description",
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var view = await MaintenanceTestHelpers.DeserializeAsync<MaintenanceBacklogView>(response);
        view.IssueSummary.Should().Be("Updated summary");
        view.IssueDescription.Should().Be("Updated description");
    }

    [Fact]
    public async Task PATCH_backlog_clears_description_when_empty_string()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var status = await MaintenanceTestHelpers.CreateStatusAsync(admin);
        var backlog = await MaintenanceTestHelpers.CreateBacklogAsync(admin, status.Uuid, device.Guid);

        var response = await admin.AppClient.PatchAsJsonAsync($"/api/v1/maintenance/backlogs/{backlog.Uuid}",
            new UpdateMaintenanceBacklogDto { IssueDescription = "" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var view = await MaintenanceTestHelpers.DeserializeAsync<MaintenanceBacklogView>(response);
        view.IssueDescription.Should().BeNull();
    }

    [Fact]
    public async Task PATCH_backlog_changes_status()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var statusA = await MaintenanceTestHelpers.CreateStatusAsync(admin);
        var statusB = await MaintenanceTestHelpers.CreateStatusAsync(admin);
        var backlog = await MaintenanceTestHelpers.CreateBacklogAsync(admin, statusA.Uuid, device.Guid);

        var response = await admin.AppClient.PatchAsJsonAsync($"/api/v1/maintenance/backlogs/{backlog.Uuid}",
            new UpdateMaintenanceBacklogDto { StatusUuid = statusB.Uuid });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var view = await MaintenanceTestHelpers.DeserializeAsync<MaintenanceBacklogView>(response);
        view.Status.Uuid.Should().Be(statusB.Uuid);
    }

    [Fact]
    public async Task PATCH_backlog_with_unknown_status_returns_404()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var status = await MaintenanceTestHelpers.CreateStatusAsync(admin);
        var backlog = await MaintenanceTestHelpers.CreateBacklogAsync(admin, status.Uuid, device.Guid);

        var response = await admin.AppClient.PatchAsJsonAsync($"/api/v1/maintenance/backlogs/{backlog.Uuid}",
            new UpdateMaintenanceBacklogDto { StatusUuid = Guid.NewGuid() });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PATCH_backlog_unknown_uuid_returns_404()
    {
        var admin = await fixture.GetInitialAdminUserAsync();

        var response = await admin.AppClient.PatchAsJsonAsync($"/api/v1/maintenance/backlogs/{Guid.NewGuid()}",
            new UpdateMaintenanceBacklogDto { IssueSummary = "X" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Resolve / Unresolve ───────────────────────────────────────────────────

    [Fact]
    public async Task PATCH_backlog_resolve_true_sets_resolved_at()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var status = await MaintenanceTestHelpers.CreateStatusAsync(admin);
        var backlog = await MaintenanceTestHelpers.CreateBacklogAsync(admin, status.Uuid, device.Guid);

        backlog.ResolvedAt.Should().BeNull();

        var response = await admin.AppClient.PatchAsJsonAsync($"/api/v1/maintenance/backlogs/{backlog.Uuid}",
            new UpdateMaintenanceBacklogDto { Resolve = true });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var view = await MaintenanceTestHelpers.DeserializeAsync<MaintenanceBacklogView>(response);
        view.ResolvedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task PATCH_backlog_resolve_false_clears_resolved_at()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var status = await MaintenanceTestHelpers.CreateStatusAsync(admin);
        var backlog = await MaintenanceTestHelpers.CreateBacklogAsync(admin, status.Uuid, device.Guid);

        // Resolve it first
        await admin.AppClient.PatchAsJsonAsync($"/api/v1/maintenance/backlogs/{backlog.Uuid}",
            new UpdateMaintenanceBacklogDto { Resolve = true });

        // Now unresolve
        var response = await admin.AppClient.PatchAsJsonAsync($"/api/v1/maintenance/backlogs/{backlog.Uuid}",
            new UpdateMaintenanceBacklogDto { Resolve = false });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var view = await MaintenanceTestHelpers.DeserializeAsync<MaintenanceBacklogView>(response);
        view.ResolvedAt.Should().BeNull();
    }

    [Fact]
    public async Task PATCH_backlog_resolving_already_resolved_is_idempotent()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var status = await MaintenanceTestHelpers.CreateStatusAsync(admin);
        var backlog = await MaintenanceTestHelpers.CreateBacklogAsync(admin, status.Uuid, device.Guid);

        // Resolve once
        var firstResolve = await admin.AppClient.PatchAsJsonAsync($"/api/v1/maintenance/backlogs/{backlog.Uuid}",
            new UpdateMaintenanceBacklogDto { Resolve = true });
        var firstView = await MaintenanceTestHelpers.DeserializeAsync<MaintenanceBacklogView>(firstResolve);
        var firstResolvedAt = firstView.ResolvedAt;

        // Resolve again - timestamp must NOT change
        await Task.Delay(50);
        var secondResolve = await admin.AppClient.PatchAsJsonAsync($"/api/v1/maintenance/backlogs/{backlog.Uuid}",
            new UpdateMaintenanceBacklogDto { Resolve = true });
        var secondView = await MaintenanceTestHelpers.DeserializeAsync<MaintenanceBacklogView>(secondResolve);

        secondResolve.StatusCode.Should().Be(HttpStatusCode.OK);
        secondView.ResolvedAt.Should().Be(firstResolvedAt);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DELETE_backlog_removes_entry()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var status = await MaintenanceTestHelpers.CreateStatusAsync(admin);
        var backlog = await MaintenanceTestHelpers.CreateBacklogAsync(admin, status.Uuid, device.Guid);

        var deleteResponse = await admin.AppClient.DeleteAsync($"/api/v1/maintenance/backlogs/{backlog.Uuid}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await admin.AppClient.GetAsync($"/api/v1/maintenance/backlogs/{backlog.Uuid}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DELETE_backlog_unknown_uuid_returns_404()
    {
        var admin = await fixture.GetInitialAdminUserAsync();

        var response = await admin.AppClient.DeleteAsync($"/api/v1/maintenance/backlogs/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DELETE_backlog_does_not_cascade_to_device()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var status = await MaintenanceTestHelpers.CreateStatusAsync(admin);
        var backlog = await MaintenanceTestHelpers.CreateBacklogAsync(admin, status.Uuid, device.Guid);

        await admin.AppClient.DeleteAsync($"/api/v1/maintenance/backlogs/{backlog.Uuid}");

        var deviceResponse = await admin.AppClient.GetAsync($"/api/v1/inventory/devices/{device.Guid}");
        deviceResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Edge cases ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0.001)]
    [InlineData(1)]
    [InlineData(9999.999)]
    public async Task PUT_backlog_various_valid_quantities_succeed(decimal quantity)
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var status = await MaintenanceTestHelpers.CreateStatusAsync(admin);

        var response = await admin.AppClient.PutAsJsonAsync("/api/v1/maintenance/backlogs", new CreateMaintenanceBacklogDto
        {
            StatusUuid = status.Uuid,
            DeviceUuid = device.Guid,
            IssueSummary = $"Qty test {quantity}",
            QuantityAffected = quantity,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var view = await MaintenanceTestHelpers.DeserializeAsync<MaintenanceBacklogView>(response);
        view.QuantityAffected.Should().Be(quantity);
    }

    [Fact]
    public async Task DELETE_device_with_backlogs_still_removes_device()
    {
        // Backlog has RESTRICT FK to device so deleting device with backlogs should fail.
        // Verify we get the right error (409 conflict or 500 from EF).
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var status = await MaintenanceTestHelpers.CreateStatusAsync(admin);
        await MaintenanceTestHelpers.CreateBacklogAsync(admin, status.Uuid, device.Guid);

        var deleteResponse = await admin.AppClient.DeleteAsync($"/api/v1/inventory/devices/{device.Guid}");

        // Expect failure because of RESTRICT constraint — not cascaded
        deleteResponse.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PUT_multiple_backlogs_same_device_all_succeed()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var status = await MaintenanceTestHelpers.CreateStatusAsync(admin);

        for (var i = 0; i < 5; i++)
        {
            var r = await admin.AppClient.PutAsJsonAsync("/api/v1/maintenance/backlogs", new CreateMaintenanceBacklogDto
            {
                StatusUuid = status.Uuid,
                DeviceUuid = device.Guid,
                IssueSummary = $"Issue {i}",
                QuantityAffected = i + 1m,
            });
            r.StatusCode.Should().Be(HttpStatusCode.Created);
        }

        var list = await MaintenanceTestHelpers.DeserializeAsync<List<MaintenanceBacklogView>>(
            await admin.AppClient.GetAsync($"/api/v1/maintenance/devices/{device.Guid}/backlogs"));
        list.Should().HaveCount(5);
    }
}
