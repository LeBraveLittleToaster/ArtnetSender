using FluentAssertions;
using LumenForgeServer.Common;
using LumenForgeServer.IntegrationTests.Collections;
using LumenForgeServer.IntegrationTests.Fixtures;
using LumenForgeServer.IntegrationTests.Inventory;
using LumenForgeServer.Inventory.Domain;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Dto.Command;
using LumenForgeServer.Rentals.Dto.View;
using NodaTime;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using LumenForgeServer.Auth.Dto.Views;

namespace LumenForgeServer.IntegrationTests.Rentals;

[Collection(AuthCollection.Name)]
public class RentalEndpointsTests(AuthFixture fixture)
{
    // =========================================================================
    // Authentication
    // =========================================================================

    [Fact]
    public async Task GET_rentals_requires_authentication()
    {
        var response = await fixture.GetAnonymousClient().GetAsync("/api/v1/rentals");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // Create
    // =========================================================================

    [Fact]
    public async Task PUT_rental_creates_and_returns_rental()
    {
        var admin = await fixture.GetInitialAdminUserAsync();

        var response = await admin.AppClient.PutAsJsonAsync("/api/v1/rentals", new CreateRentalDto
        {
            RequestTitle = "Camera kit for concert",
            EventName = "Summer Concert 2025",
            Priority = RentalPriority.HIGH,
            PlannedPickupAt = "2025-09-01T08:00:00Z",
            PlannedReturnAt = "2025-09-03T18:00:00Z",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var rental = await RentalTestHelpers.DeserializeAsync<RentalView>(response);
        rental.Uuid.Should().NotBe(Guid.Empty);
        rental.RentalStatus.Should().Be(RentalStatus.Requested);
        rental.RequestTitle.Should().Be("Camera kit for concert");
        rental.EventName.Should().Be("Summer Concert 2025");
        rental.PlannedPickupAt.Should().NotBeNull();
        rental.PlannedReturnAt.Should().NotBeNull();
    }

    [Fact]
    public async Task PUT_rental_ignores_supplied_status_and_sets_requested()
    {
        var admin = await fixture.GetInitialAdminUserAsync();

        using var content = JsonContent.Create(new
        {
            rental_status = nameof(RentalStatus.Completed),
            request_title = "Should still be requested"
        });

        var response = await admin.AppClient.PutAsync("/api/v1/rentals", content);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var rental = await RentalTestHelpers.DeserializeAsync<RentalView>(response);
        rental.RentalStatus.Should().Be(RentalStatus.Requested);
    }

    [Fact]
    public async Task PUT_rental_with_pickup_after_return_returns_bad_request()
    {
        var admin = await fixture.GetInitialAdminUserAsync();

        var response = await admin.AppClient.PutAsJsonAsync("/api/v1/rentals", new CreateRentalDto
        {
            PlannedPickupAt = "2025-09-10T00:00:00Z",
            PlannedReturnAt = "2025-09-01T00:00:00Z",
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // =========================================================================
    // Read
    // ==========================================================================

    [Fact]
    public async Task GET_rental_returns_created_rental()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var status = await RentalTestHelpers.EnsureRentalStatusAsync();
        var created = await RentalTestHelpers.CreateRentalAsync(admin, status);

        var response = await admin.AppClient.GetAsync($"/api/v1/rentals/{created.Uuid}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rental = await RentalTestHelpers.DeserializeAsync<RentalView>(response);
        rental.Uuid.Should().Be(created.Uuid);
        rental.RentalStatus.Should().Be(RentalStatus.Requested);
    }

    [Fact]
    public async Task GET_rental_unknown_uuid_returns_not_found()
    {
        var admin = await fixture.GetInitialAdminUserAsync();

        var response = await admin.AppClient.GetAsync($"/api/v1/rentals/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_rentals_supports_search_and_paging()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var status = await RentalTestHelpers.EnsureRentalStatusAsync();
        var marker = $"SearchMarker-{Guid.NewGuid():N}";
        _ = await RentalTestHelpers.CreateRentalAsync(admin, status, title: marker);

        var response = await admin.AppClient.GetAsync(
            $"/api/v1/rentals?search={marker}&limit=10&offset=0");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = JsonSerializer.Deserialize<JsonElement>(
            await response.Content.ReadAsStringAsync());
        var total = body.GetProperty("total").GetInt64();
        var list = body.GetProperty("list")
            .Deserialize<List<RentalView>>(Json.GetJsonSerializerOptions())!;
        total.Should().BeGreaterThan(0);
        list.Should().Contain(r => r.RequestTitle == marker);
    }

    [Fact]
    public async Task GET_rentals_pagination_limits_page_size()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var status = await RentalTestHelpers.EnsureRentalStatusAsync();

        for (var i = 0; i < 3; i++)
            _ = await RentalTestHelpers.CreateRentalAsync(admin, status);

        var response = await admin.AppClient.GetAsync("/api/v1/rentals?limit=2&offset=0");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = JsonSerializer.Deserialize<JsonElement>(
            await response.Content.ReadAsStringAsync());
        var total = body.GetProperty("total").GetInt64();
        var list = body.GetProperty("list")
            .Deserialize<List<RentalView>>(Json.GetJsonSerializerOptions())!;

        total.Should().BeGreaterThanOrEqualTo(3);
        list.Should().HaveCount(2);
    }

    // =========================================================================
    // Update
    // =========================================================================

    [Fact]
    public async Task PATCH_rental_updates_title_and_priority()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var status = await RentalTestHelpers.EnsureRentalStatusAsync();
        var rental = await RentalTestHelpers.CreateRentalAsync(admin, status);

        var response = await admin.AppClient.PatchAsJsonAsync(
            $"/api/v1/rentals/{rental.Uuid}",
            new UpdateRentalDto
            {
                RequestTitle = "Updated title",
                Priority = RentalPriority.URGENT,
                CustomerNotes = "Urgent — event moved up",
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await RentalTestHelpers.DeserializeAsync<RentalView>(response);
        updated.RequestTitle.Should().Be("Updated title");
        updated.Priority.Should().Be(RentalPriority.URGENT);
        updated.CustomerNotes.Should().Be("Urgent — event moved up");
    }

    [Fact]
    public async Task PATCH_rental_with_unknown_uuid_returns_not_found()
    {
        var admin = await fixture.GetInitialAdminUserAsync();

        var response = await admin.AppClient.PatchAsJsonAsync(
            $"/api/v1/rentals/{Guid.NewGuid()}",
            new UpdateRentalDto { RequestTitle = "ghost" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Delete
    // =========================================================================

    [Fact]
    public async Task DELETE_rental_removes_it()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var status = await RentalTestHelpers.EnsureRentalStatusAsync();
        var rental = await RentalTestHelpers.CreateRentalAsync(admin, status);

        var deleteResponse = await admin.AppClient.DeleteAsync($"/api/v1/rentals/{rental.Uuid}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await admin.AppClient.GetAsync($"/api/v1/rentals/{rental.Uuid}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Conflict check
    // =========================================================================

    [Fact]
    public async Task GET_conflicts_with_unknown_device_returns_not_found()
    {
        var admin = await fixture.GetInitialAdminUserAsync();

        var response = await admin.AppClient.GetAsync(
            $"/api/v1/rentals/conflicts?device_guid={Guid.NewGuid()}" +
            $"&start=2025-01-01T00:00:00Z&end=2025-01-07T00:00:00Z");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(Skip = "Stock binding endpoint not yet implemented in DeviceController")]
    public async Task GET_conflicts_returns_empty_when_no_overlap()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);

        // Bind device Jan 1–7
        var bindStart = Instant.FromUtc(2025, 1, 1, 0, 0);
        var bindEnd = Instant.FromUtc(2025, 1, 7, 0, 0);
        _ = await InventoryTestHelpers.CreateStockBindingAsync(
            admin, device.Guid, BindingType.RENTAL_REQUEST, bindStart, bindEnd);

        // Query a completely separate window (Jan 10–15)
        var response = await admin.AppClient.GetAsync(
            $"/api/v1/rentals/conflicts?device_guid={device.Guid}" +
            $"&start=2025-01-10T00:00:00Z&end=2025-01-15T00:00:00Z" +
            $"&binding_type=601"); // RENTAL_REQUEST

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = JsonSerializer.Deserialize<JsonElement>(
            await response.Content.ReadAsStringAsync());
        body.GetProperty("total").GetInt64().Should().Be(0);
    }

    [Fact(Skip = "Stock binding endpoint not yet implemented in DeviceController")]
    public async Task GET_conflicts_detects_overlapping_binding()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);

        // Bind device Mar 5–10
        var bindStart = Instant.FromUtc(2025, 3, 5, 0, 0);
        var bindEnd = Instant.FromUtc(2025, 3, 10, 0, 0);
        _ = await InventoryTestHelpers.CreateStockBindingAsync(
            admin, device.Guid, BindingType.RENTAL_REQUEST, bindStart, bindEnd);

        // Query window Mar 3–8 — overlaps
        var response = await admin.AppClient.GetAsync(
            $"/api/v1/rentals/conflicts?device_guid={device.Guid}" +
            $"&start=2025-03-03T00:00:00Z&end=2025-03-08T00:00:00Z" +
            $"&binding_type=601");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = JsonSerializer.Deserialize<JsonElement>(
            await response.Content.ReadAsStringAsync());
        var total = body.GetProperty("total").GetInt64();
        var list = body.GetProperty("list")
            .Deserialize<List<StockBindingConflictView>>(Json.GetJsonSerializerOptions())!;

        total.Should().Be(1);
        list.Should().ContainSingle(c => c.DeviceGuid == device.Guid);
    }

    [Fact(Skip = "Stock binding endpoint not yet implemented in DeviceController")]
    public async Task GET_conflicts_supports_pagination()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var deviceA = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var deviceB = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);

        var start = Instant.FromUtc(2025, 4, 1, 0, 0);
        var end = Instant.FromUtc(2025, 4, 10, 0, 0);
        _ = await InventoryTestHelpers.CreateStockBindingAsync(
            admin, deviceA.Guid, BindingType.RENTAL_REQUEST, start, end);
        _ = await InventoryTestHelpers.CreateStockBindingAsync(
            admin, deviceB.Guid, BindingType.RENTAL_REQUEST, start, end);

        // Fetch first page of 1
        var r1 = await admin.AppClient.GetAsync(
            $"/api/v1/rentals/conflicts?device_guid={deviceA.Guid}" +
            $"&start=2025-04-01T00:00:00Z&end=2025-04-10T00:00:00Z" +
            $"&binding_type=601&limit=1&offset=0");

        r1.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = JsonSerializer.Deserialize<JsonElement>(
            await r1.Content.ReadAsStringAsync());
        body.GetProperty("list")
            .Deserialize<List<StockBindingConflictView>>(Json.GetJsonSerializerOptions())!
            .Should().HaveCount(1);
    }

    // =========================================================================
    // Statuses lookup
    // =========================================================================

    [Fact]
    public async Task GET_rental_statuses_returns_lookup_list()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var response = await admin.AppClient.GetAsync("/api/v1/rentals/statuses");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
        body.GetProperty("total").GetInt64().Should().BeGreaterThan(0);

        var statuses = body.GetProperty("list")
            .Deserialize<List<RentalStatus>>(Json.GetJsonSerializerOptions())!;

        statuses.Should().Contain(RentalStatus.Approved);
        statuses.Should().Contain(RentalStatus.Requested);
    }

    // =========================================================================
    // Actions — available
    // =========================================================================

    [Fact]
    public async Task GET_available_actions_for_requested_rental_includes_approve_and_reject()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var rental = await RentalTestHelpers.CreateRentalAsync(admin, RentalStatus.Requested);

        var response = await admin.AppClient.GetAsync(
            $"/api/v1/rentals/{rental.Uuid}/actions/available");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var available = await RentalTestHelpers.DeserializeAsync<ListViewDto<AvailableActionView>>(response);

        available.list.Should().Contain(a => a.ActionType == ActionType.ApproveRequest);
        available.list.Should().Contain(a => a.ActionType == ActionType.RejectRequest);
        available.list.Should().Contain(a => a.ActionType == ActionType.CancelRental);
    }

    [Fact]
    public async Task GET_available_actions_for_unknown_rental_returns_not_found()
    {
        var admin = await fixture.GetInitialAdminUserAsync();

        var response = await admin.AppClient.GetAsync(
            $"/api/v1/rentals/{Guid.NewGuid()}/actions/available");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Actions — execute
    // =========================================================================

    [Fact]
    public async Task POST_action_approve_request_transitions_rental_to_approved()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var rental = await RentalTestHelpers.CreateRentalAsync(admin, RentalStatus.Requested);

        var response = await admin.AppClient.PostAsJsonAsync(
            $"/api/v1/rentals/{rental.Uuid}/actions",
            new ExecuteActionDto { ActionType = ActionType.ApproveRequest });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var action = await RentalTestHelpers.DeserializeAsync<RentalActionView>(response);
        action.ActionType.Should().Be(ActionType.ApproveRequest);
        action.Uuid.Should().NotBe(Guid.Empty);
        action.PerformedByUserId.Should().NotBeNullOrEmpty();

        // Verify the rental status was actually updated
        var getResponse = await admin.AppClient.GetAsync($"/api/v1/rentals/{rental.Uuid}");
        var updated = await RentalTestHelpers.DeserializeAsync<RentalView>(getResponse);
        updated.RentalStatus.Should().Be(RentalStatus.Approved);
        updated.AssignedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task POST_action_reject_request_transitions_rental_to_rejected()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var rental = await RentalTestHelpers.CreateRentalAsync(admin, RentalStatus.Requested);

        var response = await admin.AppClient.PostAsJsonAsync(
            $"/api/v1/rentals/{rental.Uuid}/actions",
            new ExecuteActionDto
            {
                ActionType = ActionType.RejectRequest,
                Input = JsonSerializer.SerializeToElement(new { reason = "Out of stock" }),
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var action = await RentalTestHelpers.DeserializeAsync<RentalActionView>(response);
        action.ActionType.Should().Be(ActionType.RejectRequest);

        var getResponse = await admin.AppClient.GetAsync($"/api/v1/rentals/{rental.Uuid}");
        var updated = await RentalTestHelpers.DeserializeAsync<RentalView>(getResponse);
        updated.RentalStatus.Should().Be(RentalStatus.Rejected);
    }

    [Fact]
    public async Task POST_action_returns_bad_request_when_action_not_available()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var rental = await RentalTestHelpers.CreateRentalAsync(admin, RentalStatus.Requested);

        // CompleteRental is not available from Requested status
        var response = await admin.AppClient.PostAsJsonAsync(
            $"/api/v1/rentals/{rental.Uuid}/actions",
            new ExecuteActionDto { ActionType = ActionType.CompleteRental });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_action_for_unknown_rental_returns_not_found()
    {
        var admin = await fixture.GetInitialAdminUserAsync();

        var response = await admin.AppClient.PostAsJsonAsync(
            $"/api/v1/rentals/{Guid.NewGuid()}/actions",
            new ExecuteActionDto { ActionType = ActionType.ApproveRequest });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_action_cancel_rental_from_requested_transitions_to_cancelled()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var rental = await RentalTestHelpers.CreateRentalAsync(admin, RentalStatus.Requested);

        var response = await admin.AppClient.PostAsJsonAsync(
            $"/api/v1/rentals/{rental.Uuid}/actions",
            new ExecuteActionDto
            {
                ActionType = ActionType.CancelRental,
                Input = JsonSerializer.SerializeToElement(new { reason = "Customer withdrew" }),
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var action = await RentalTestHelpers.DeserializeAsync<RentalActionView>(response);
        action.ActionType.Should().Be(ActionType.CancelRental);

        var getResponse = await admin.AppClient.GetAsync($"/api/v1/rentals/{rental.Uuid}");
        var updated = await RentalTestHelpers.DeserializeAsync<RentalView>(getResponse);
        updated.RentalStatus.Should().Be(RentalStatus.Cancelled);
    }

    // =========================================================================
    // Actions — history
    // =========================================================================

    [Fact]
    public async Task GET_action_history_returns_executed_actions()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var rental = await RentalTestHelpers.CreateRentalAsync(admin, RentalStatus.Requested);

        // Execute an action first
        _ = await admin.AppClient.PostAsJsonAsync(
            $"/api/v1/rentals/{rental.Uuid}/actions",
            new ExecuteActionDto { ActionType = ActionType.ApproveRequest });

        var response = await admin.AppClient.GetAsync(
            $"/api/v1/rentals/{rental.Uuid}/actions?limit=50&offset=0");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var history = await RentalTestHelpers.DeserializeAsync<ListViewDto<RentalActionView>>(response);
        history.total.Should().BeGreaterThanOrEqualTo(1);
        history.list.Should().Contain(a => a.ActionType == ActionType.ApproveRequest);
    }

    [Fact]
    public async Task GET_action_history_for_unknown_rental_returns_not_found()
    {
        var admin = await fixture.GetInitialAdminUserAsync();

        var response = await admin.AppClient.GetAsync(
            $"/api/v1/rentals/{Guid.NewGuid()}/actions?limit=50&offset=0");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_available_actions_updates_after_status_change()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var rental = await RentalTestHelpers.CreateRentalAsync(admin, RentalStatus.Requested);

        // Approve the rental
        _ = await admin.AppClient.PostAsJsonAsync(
            $"/api/v1/rentals/{rental.Uuid}/actions",
            new ExecuteActionDto { ActionType = ActionType.ApproveRequest });

        // Available actions should now reflect Approved status
        var response = await admin.AppClient.GetAsync(
            $"/api/v1/rentals/{rental.Uuid}/actions/available");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var available = await RentalTestHelpers.DeserializeAsync<ListViewDto<AvailableActionView>>(response);

        // ApproveRequest should no longer be available
        available.list.Should().NotContain(a => a.ActionType == ActionType.ApproveRequest);
        // RecordPickup should now be available from Approved status
        available.list.Should().Contain(a => a.ActionType == ActionType.RecordPickup);
    }
}
