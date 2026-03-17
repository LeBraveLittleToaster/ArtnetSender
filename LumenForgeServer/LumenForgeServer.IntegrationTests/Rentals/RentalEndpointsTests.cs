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
        var status = await RentalTestHelpers.EnsureRentalStatusAsync();

        var response = await admin.AppClient.PutAsJsonAsync("/api/v1/rentals", new CreateRentalDto
        {
            RentalStatus = status,
            RequestTitle = "Camera kit for concert",
            EventName = "Summer Concert 2025",
            Priority = RentalPriority.HIGH,
            PlannedPickupAt = "2025-09-01T08:00:00Z",
            PlannedReturnAt = "2025-09-03T18:00:00Z",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var rental = await RentalTestHelpers.DeserializeAsync<RentalView>(response);
        rental.Uuid.Should().NotBe(Guid.Empty);
        rental.RentalStatus.Should().Be(status);
        rental.RequestTitle.Should().Be("Camera kit for concert");
        rental.EventName.Should().Be("Summer Concert 2025");
        rental.PlannedPickupAt.Should().NotBeNull();
        rental.PlannedReturnAt.Should().NotBeNull();
    }

    [Fact]
    public async Task PUT_rental_with_invalid_status_returns_bad_request()
    {
        var admin = await fixture.GetInitialAdminUserAsync();

        using var content = JsonContent.Create(new
        {
            rental_status = "InvalidStatus",
            request_title = "Should fail"
        });

        var response = await admin.AppClient.PutAsync("/api/v1/rentals", content);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PUT_rental_with_pickup_after_return_returns_bad_request()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var status = await RentalTestHelpers.EnsureRentalStatusAsync();

        var response = await admin.AppClient.PutAsJsonAsync("/api/v1/rentals", new CreateRentalDto
        {
            RentalStatus = status,
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
        rental.RentalStatus.Should().Be(status);
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
    // Status state-machine
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

    [Fact]
    public async Task GET_rental_transitions_returns_allowed_targets_for_current_state()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var requested = await RentalTestHelpers.EnsureRentalStatusByNameAsync("Requested");

        var rental = await RentalTestHelpers.CreateRentalAsync(admin, requested);

        var response = await admin.AppClient.GetAsync($"/api/v1/rentals/{rental.Uuid}/transitions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());

        Enum.Parse<RentalStatus>(body.GetProperty("current").GetString()!).Should().Be(RentalStatus.Requested);

        var allowed = body.GetProperty("allowed")
            .Deserialize<List<RentalStatus>>(Json.GetJsonSerializerOptions())!;

        allowed.Should().Contain(RentalStatus.Approved);
        allowed.Should().Contain(RentalStatus.Rejected);
        allowed.Should().Contain(RentalStatus.Cancelled);
    }

    [Fact]
    public async Task POST_rental_transition_updates_status_when_transition_is_allowed()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var rental = await RentalTestHelpers.CreateRentalAsync(admin, RentalStatus.Requested);

        var response = await admin.AppClient.PostAsJsonAsync(
            $"/api/v1/rentals/{rental.Uuid}/transitions",
            new TransitionRentalStatusDto { TargetStatus = RentalStatus.Approved });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await RentalTestHelpers.DeserializeAsync<RentalView>(response);
        updated.RentalStatus.Should().Be(RentalStatus.Approved);
        updated.AssignedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task POST_rental_transition_returns_bad_request_for_invalid_transition()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var rental = await RentalTestHelpers.CreateRentalAsync(admin, RentalStatus.Requested);

        var response = await admin.AppClient.PostAsJsonAsync(
            $"/api/v1/rentals/{rental.Uuid}/transitions",
            new TransitionRentalStatusDto { TargetStatus = RentalStatus.Completed });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
