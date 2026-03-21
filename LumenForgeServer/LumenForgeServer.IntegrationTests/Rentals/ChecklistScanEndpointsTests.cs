#if false // Disabled: references removed Dto.View types from pre-rework rental API
using FluentAssertions;
using LumenForgeServer.IntegrationTests.Collections;
using LumenForgeServer.IntegrationTests.Fixtures;
using LumenForgeServer.IntegrationTests.Inventory;
using LumenForgeServer.Rentals.Dto.Command;
using LumenForgeServer.Rentals.Dto.View;
using System.Net;
using System.Net.Http.Json;

namespace LumenForgeServer.IntegrationTests.Rentals;

[Collection(AuthCollection.Name)]
public class ChecklistScanEndpointsTests(AuthFixture fixture)
{
    // =========================================================================
    // Authentication
    // =========================================================================

    [Fact]
    public async Task GET_scan_requires_authentication()
    {
        var response = await fixture.GetAnonymousClient()
            .GetAsync(RentalTestHelpers.GetChecklistScanPath(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // Happy path
    // =========================================================================

    [Fact(Skip = "Stock binding endpoint not yet implemented in DeviceController")]
    public async Task GET_scan_returns_checklist_item_for_scanned_device()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var statusGuid = await RentalTestHelpers.EnsureRentalStatusAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);

        var (rental, rentalItemUuid, deviceGuid) =
            await RentalTestHelpers.SeedRentalWithApprovedItemAndDeviceAsync(admin, statusGuid, vendor.Guid);

        var checklist = await RentalTestHelpers.GeneratePickupChecklistAsync(admin, rental.Uuid);

        var response = await admin.AppClient.GetAsync(
            RentalTestHelpers.GetChecklistScanPath(rental.Uuid, checklist.Uuid, deviceGuid));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var item = await RentalTestHelpers.DeserializeAsync<ChecklistItemView>(response);
        item.Uuid.Should().NotBe(Guid.Empty);
        item.RentalItemUuid.Should().Be(rentalItemUuid);
    }

    [Fact(Skip = "Stock binding endpoint not yet implemented in DeviceController")]
    public async Task GET_scan_returns_item_with_is_checked_false_before_inspection()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var statusGuid = await RentalTestHelpers.EnsureRentalStatusAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);

        var (rental, _, deviceGuid) =
            await RentalTestHelpers.SeedRentalWithApprovedItemAndDeviceAsync(admin, statusGuid, vendor.Guid);

        var checklist = await RentalTestHelpers.GeneratePickupChecklistAsync(admin, rental.Uuid);

        var response = await admin.AppClient.GetAsync(
            RentalTestHelpers.GetChecklistScanPath(rental.Uuid, checklist.Uuid, deviceGuid));

        var item = await RentalTestHelpers.DeserializeAsync<ChecklistItemView>(response);
        item.IsChecked.Should().BeFalse();
        item.QuantityChecked.Should().Be(0);
    }

    [Fact(Skip = "Stock binding endpoint not yet implemented in DeviceController")]
    public async Task GET_scan_reflects_updated_state_after_item_is_checked()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var statusGuid = await RentalTestHelpers.EnsureRentalStatusAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);

        var (rental, _, deviceGuid) =
            await RentalTestHelpers.SeedRentalWithApprovedItemAndDeviceAsync(admin, statusGuid, vendor.Guid);

        var checklist = await RentalTestHelpers.GeneratePickupChecklistAsync(admin, rental.Uuid);

        // Locate the item via the scan endpoint, then submit an inspection result
        var scanResponse = await admin.AppClient.GetAsync(
            RentalTestHelpers.GetChecklistScanPath(rental.Uuid, checklist.Uuid, deviceGuid));
        var item = await RentalTestHelpers.DeserializeAsync<ChecklistItemView>(scanResponse);

        _ = await admin.AppClient.PatchAsJsonAsync(
            RentalTestHelpers.GetChecklistItemPath(rental.Uuid, checklist.Uuid, item.Uuid),
            new UpdateChecklistItemDto { QuantityChecked = 1, ConditionOk = true });

        // Scan again — the returned item must now show is_checked = true
        var rescanResponse = await admin.AppClient.GetAsync(
            RentalTestHelpers.GetChecklistScanPath(rental.Uuid, checklist.Uuid, deviceGuid));

        rescanResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var rescanned = await RentalTestHelpers.DeserializeAsync<ChecklistItemView>(rescanResponse);
        rescanned.IsChecked.Should().BeTrue();
        rescanned.QuantityChecked.Should().Be(1);
    }

    // =========================================================================
    // Error cases
    // =========================================================================

    [Fact(Skip = "Stock binding endpoint not yet implemented in DeviceController")]
    public async Task GET_scan_with_device_not_on_checklist_returns_not_found()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var statusGuid = await RentalTestHelpers.EnsureRentalStatusAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);

        // Create a rental item linked to deviceA
        var (rental, _, _) =
            await RentalTestHelpers.SeedRentalWithApprovedItemAndDeviceAsync(admin, statusGuid, vendor.Guid);
        var checklist = await RentalTestHelpers.GeneratePickupChecklistAsync(admin, rental.Uuid);

        // Scan an entirely different device that has no rental item on this checklist
        var unrelatedDevice = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);
        var response = await admin.AppClient.GetAsync(
            RentalTestHelpers.GetChecklistScanPath(rental.Uuid, checklist.Uuid, unrelatedDevice.Guid));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_scan_with_unknown_checklist_returns_not_found()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var statusGuid = await RentalTestHelpers.EnsureRentalStatusAsync();
        var rental = await RentalTestHelpers.CreateRentalAsync(admin, statusGuid);
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);

        var response = await admin.AppClient.GetAsync(
            RentalTestHelpers.GetChecklistScanPath(rental.Uuid, Guid.NewGuid(), device.Guid));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_scan_with_unknown_rental_returns_not_found()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);

        var response = await admin.AppClient.GetAsync(
            RentalTestHelpers.GetChecklistScanPath(Guid.NewGuid(), Guid.NewGuid(), device.Guid));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(Skip = "Stock binding endpoint not yet implemented in DeviceController")]
    public async Task GET_scan_on_signed_checklist_returns_bad_request()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var statusGuid = await RentalTestHelpers.EnsureRentalStatusAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);

        var (rental, _, deviceGuid) =
            await RentalTestHelpers.SeedRentalWithApprovedItemAndDeviceAsync(admin, statusGuid, vendor.Guid);
        var checklist = await RentalTestHelpers.GeneratePickupChecklistAsync(admin, rental.Uuid);

        // Sign the checklist to make it immutable
        _ = await admin.AppClient.PostAsJsonAsync(
            RentalTestHelpers.GetChecklistSignPath(rental.Uuid, checklist.Uuid),
            new SignChecklistDto());

        // Scanning a signed checklist must be rejected
        var response = await admin.AppClient.GetAsync(
            RentalTestHelpers.GetChecklistScanPath(rental.Uuid, checklist.Uuid, deviceGuid));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(Skip = "Stock binding endpoint not yet implemented in DeviceController")]
    public async Task GET_scan_device_from_wrong_rental_returns_not_found()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var statusGuid = await RentalTestHelpers.EnsureRentalStatusAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);

        // Create two independent rentals each with their own device
        var (rentalA, _, deviceGuidA) =
            await RentalTestHelpers.SeedRentalWithApprovedItemAndDeviceAsync(admin, statusGuid, vendor.Guid);
        var (rentalB, _, _) =
            await RentalTestHelpers.SeedRentalWithApprovedItemAndDeviceAsync(admin, statusGuid, vendor.Guid);

        var checklistA = await RentalTestHelpers.GeneratePickupChecklistAsync(admin, rentalA.Uuid);
        var checklistB = await RentalTestHelpers.GeneratePickupChecklistAsync(admin, rentalB.Uuid);

        // Scan rentalA's device against rentalB's checklist — must not match
        var response = await admin.AppClient.GetAsync(
            RentalTestHelpers.GetChecklistScanPath(rentalB.Uuid, checklistB.Uuid, deviceGuidA));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
#endif
