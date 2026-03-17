using FluentAssertions;
using LumenForgeServer.Auth.Dto.Views;
using LumenForgeServer.Common;
using LumenForgeServer.IntegrationTests.Collections;
using LumenForgeServer.IntegrationTests.Fixtures;
using LumenForgeServer.Rentals.Dto.Command;
using LumenForgeServer.Rentals.Dto.View;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace LumenForgeServer.IntegrationTests.Rentals;

[Collection(AuthCollection.Name)]
public class ChecklistEndpointsTests(AuthFixture fixture)
{
    // =========================================================================
    // Authentication
    // =========================================================================

    [Fact]
    public async Task GET_checklists_requires_authentication()
    {
        var response = await fixture.GetAnonymousClient()
            .GetAsync($"/api/v1/rentals/{Guid.NewGuid()}/checklists");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // Generate PICKUP
    // =========================================================================

    [Fact]
    public async Task POST_generate_pickup_without_approved_items_returns_bad_request()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var statusGuid = await RentalTestHelpers.EnsureRentalStatusAsync();
        var rental = await RentalTestHelpers.CreateRentalAsync(admin, statusGuid);

        // No approved items seeded — service must reject
        var response = await admin.AppClient.PostAsJsonAsync(
            $"/api/v1/rentals/{rental.Uuid}/checklists/generate",
            new GenerateChecklistDto { ChecklistType = ChecklistType.PICKUP });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_generate_pickup_creates_checklist_with_all_items_unchecked()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var statusGuid = await RentalTestHelpers.EnsureRentalStatusAsync();
        var (rental, _) = await RentalTestHelpers.SeedRentalWithApprovedItemAsync(admin, statusGuid);

        var checklist = await RentalTestHelpers.GeneratePickupChecklistAsync(admin, rental.Uuid);

        checklist.Uuid.Should().NotBe(Guid.Empty);
        checklist.ChecklistType.Should().Be(ChecklistType.PICKUP);
        checklist.IsSigned.Should().BeFalse();
        checklist.IsComplete.Should().BeFalse();
        checklist.TotalItems.Should().Be(1);
        checklist.CheckedItemsCount.Should().Be(0);
        checklist.Items.Should().HaveCount(1);
        checklist.Items[0].IsChecked.Should().BeFalse();
    }

    [Fact]
    public async Task POST_generate_pickup_for_unknown_rental_returns_not_found()
    {
        var admin = await fixture.GetInitialAdminUserAsync();

        var response = await admin.AppClient.PostAsJsonAsync(
            $"/api/v1/rentals/{Guid.NewGuid()}/checklists/generate",
            new GenerateChecklistDto { ChecklistType = ChecklistType.PICKUP });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // List and Get
    // =========================================================================

    [Fact]
    public async Task GET_checklists_lists_all_for_rental()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var statusGuid = await RentalTestHelpers.EnsureRentalStatusAsync();
        var (rental, _) = await RentalTestHelpers.SeedRentalWithApprovedItemAsync(admin, statusGuid);
        _ = await RentalTestHelpers.GeneratePickupChecklistAsync(admin, rental.Uuid);

        var response = await admin.AppClient.GetAsync(
            $"/api/v1/rentals/{rental.Uuid}/checklists");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var checklists = await RentalTestHelpers.DeserializeAsync<ListViewDto<ChecklistView>>(response);
        checklists.list.Should().HaveCount(1);
        checklists.list[0].ChecklistType.Should().Be(ChecklistType.PICKUP);
    }

    [Fact]
    public async Task GET_checklist_returns_full_detail_with_items()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var statusGuid = await RentalTestHelpers.EnsureRentalStatusAsync();
        var (rental, rentalItemUuid) = await RentalTestHelpers.SeedRentalWithApprovedItemAsync(admin, statusGuid);
        var checklist = await RentalTestHelpers.GeneratePickupChecklistAsync(admin, rental.Uuid);

        var response = await admin.AppClient.GetAsync(
            $"/api/v1/rentals/{rental.Uuid}/checklists/{checklist.Uuid}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await RentalTestHelpers.DeserializeAsync<ChecklistView>(response);
        detail.Uuid.Should().Be(checklist.Uuid);
        detail.Items.Should().HaveCount(1);
        detail.Items[0].RentalItemUuid.Should().Be(rentalItemUuid);
        detail.Items[0].IsChecked.Should().BeFalse();
    }

    [Fact]
    public async Task GET_checklist_unknown_uuid_returns_not_found()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var statusGuid = await RentalTestHelpers.EnsureRentalStatusAsync();
        var rental = await RentalTestHelpers.CreateRentalAsync(admin, statusGuid);

        var response = await admin.AppClient.GetAsync(
            $"/api/v1/rentals/{rental.Uuid}/checklists/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Update items (partial completion)
    // =========================================================================

    [Fact]
    public async Task PATCH_checklist_item_marks_it_as_checked()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var statusGuid = await RentalTestHelpers.EnsureRentalStatusAsync();
        var (rental, _) = await RentalTestHelpers.SeedRentalWithApprovedItemAsync(admin, statusGuid);
        var checklist = await RentalTestHelpers.GeneratePickupChecklistAsync(admin, rental.Uuid);
        var itemUuid = checklist.Items[0].Uuid;

        var response = await admin.AppClient.PatchAsJsonAsync(
            $"/api/v1/rentals/{rental.Uuid}/checklists/{checklist.Uuid}/items/{itemUuid}",
            new UpdateChecklistItemDto
            {
                QuantityChecked = 2,
                ConditionOk = true,
                ConditionNotes = "No visible damage",
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var item = await RentalTestHelpers.DeserializeAsync<ChecklistItemView>(response);
        item.IsChecked.Should().BeTrue();
        item.QuantityChecked.Should().Be(2);
        item.ConditionOk.Should().BeTrue();
        item.ConditionNotes.Should().Be("No visible damage");
    }

    [Fact]
    public async Task PATCH_checklist_item_with_damage_records_damage_fields()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var statusGuid = await RentalTestHelpers.EnsureRentalStatusAsync();
        var (rental, _) = await RentalTestHelpers.SeedRentalWithApprovedItemAsync(admin, statusGuid);
        var checklist = await RentalTestHelpers.GeneratePickupChecklistAsync(admin, rental.Uuid);
        var itemUuid = checklist.Items[0].Uuid;

        var response = await admin.AppClient.PatchAsJsonAsync(
            $"/api/v1/rentals/{rental.Uuid}/checklists/{checklist.Uuid}/items/{itemUuid}",
            new UpdateChecklistItemDto
            {
                QuantityChecked = 1,
                ConditionOk = false,
                DamagedQuantity = 1,
                DamageSummary = "Cracked screen",
                DamageDescription = "Screen cracked on top-right corner, device still functional",
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var item = await RentalTestHelpers.DeserializeAsync<ChecklistItemView>(response);
        item.IsChecked.Should().BeTrue();
        item.ConditionOk.Should().BeFalse();
        item.DamagedQuantity.Should().Be(1);
        item.DamageSummary.Should().Be("Cracked screen");
    }

    [Fact]
    public async Task PATCH_checklist_item_updates_are_reflected_in_get_checklist()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var statusGuid = await RentalTestHelpers.EnsureRentalStatusAsync();
        var (rental, _) = await RentalTestHelpers.SeedRentalWithApprovedItemAsync(admin, statusGuid);
        var checklist = await RentalTestHelpers.GeneratePickupChecklistAsync(admin, rental.Uuid);

        // Submit inspection for the single item
        _ = await admin.AppClient.PatchAsJsonAsync(
            $"/api/v1/rentals/{rental.Uuid}/checklists/{checklist.Uuid}/items/{checklist.Items[0].Uuid}",
            new UpdateChecklistItemDto { QuantityChecked = 2, ConditionOk = true });

        // Reload checklist — is_complete and checked_items_count must reflect the update
        var response = await admin.AppClient.GetAsync(
            $"/api/v1/rentals/{rental.Uuid}/checklists/{checklist.Uuid}");

        var updated = await RentalTestHelpers.DeserializeAsync<ChecklistView>(response);
        updated.CheckedItemsCount.Should().Be(1);
        updated.TotalItems.Should().Be(1);
        updated.IsComplete.Should().BeTrue();
    }

    [Fact]
    public async Task PATCH_item_on_unknown_checklist_returns_not_found()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var statusGuid = await RentalTestHelpers.EnsureRentalStatusAsync();
        var rental = await RentalTestHelpers.CreateRentalAsync(admin, statusGuid);

        var response = await admin.AppClient.PatchAsJsonAsync(
            $"/api/v1/rentals/{rental.Uuid}/checklists/{Guid.NewGuid()}/items/{Guid.NewGuid()}",
            new UpdateChecklistItemDto { QuantityChecked = 1, ConditionOk = true });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // =========================================================================
    // Sign
    // =========================================================================

    [Fact]
    public async Task POST_sign_finalises_checklist()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var statusGuid = await RentalTestHelpers.EnsureRentalStatusAsync();
        var (rental, _) = await RentalTestHelpers.SeedRentalWithApprovedItemAsync(admin, statusGuid);
        var checklist = await RentalTestHelpers.GeneratePickupChecklistAsync(admin, rental.Uuid);

        var response = await admin.AppClient.PostAsJsonAsync(
            $"/api/v1/rentals/{rental.Uuid}/checklists/{checklist.Uuid}/sign",
            new SignChecklistDto { Notes = "All items handed over." });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var signed = await RentalTestHelpers.DeserializeAsync<ChecklistView>(response);
        signed.IsSigned.Should().BeTrue();
        signed.SignedAt.Should().NotBeNull();
        signed.SignedByUserId.Should().NotBeNullOrEmpty();
        signed.Notes.Should().Be("All items handed over.");
    }

    [Fact]
    public async Task POST_sign_is_allowed_with_partial_completion()
    {
        // Signing must succeed even when not all items have been checked (partial completion)
        var admin = await fixture.GetInitialAdminUserAsync();
        var statusGuid = await RentalTestHelpers.EnsureRentalStatusAsync();
        var (rental, _) = await RentalTestHelpers.SeedRentalWithApprovedItemAsync(admin, statusGuid);
        var checklist = await RentalTestHelpers.GeneratePickupChecklistAsync(admin, rental.Uuid);

        // Sign without checking any items
        var response = await admin.AppClient.PostAsJsonAsync(
            $"/api/v1/rentals/{rental.Uuid}/checklists/{checklist.Uuid}/sign",
            new SignChecklistDto());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var signed = await RentalTestHelpers.DeserializeAsync<ChecklistView>(response);
        signed.IsSigned.Should().BeTrue();
        signed.IsComplete.Should().BeFalse(); // still not complete — item was never checked
    }

    [Fact]
    public async Task POST_sign_twice_returns_bad_request()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var statusGuid = await RentalTestHelpers.EnsureRentalStatusAsync();
        var (rental, _) = await RentalTestHelpers.SeedRentalWithApprovedItemAsync(admin, statusGuid);
        var checklist = await RentalTestHelpers.GeneratePickupChecklistAsync(admin, rental.Uuid);

        _ = await admin.AppClient.PostAsJsonAsync(
            $"/api/v1/rentals/{rental.Uuid}/checklists/{checklist.Uuid}/sign",
            new SignChecklistDto());

        var secondSign = await admin.AppClient.PostAsJsonAsync(
            $"/api/v1/rentals/{rental.Uuid}/checklists/{checklist.Uuid}/sign",
            new SignChecklistDto());

        secondSign.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PATCH_item_on_signed_checklist_returns_bad_request()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var statusGuid = await RentalTestHelpers.EnsureRentalStatusAsync();
        var (rental, _) = await RentalTestHelpers.SeedRentalWithApprovedItemAsync(admin, statusGuid);
        var checklist = await RentalTestHelpers.GeneratePickupChecklistAsync(admin, rental.Uuid);

        // Sign first
        _ = await admin.AppClient.PostAsJsonAsync(
            $"/api/v1/rentals/{rental.Uuid}/checklists/{checklist.Uuid}/sign",
            new SignChecklistDto());

        // Attempt to update item on the now-signed checklist
        var response = await admin.AppClient.PatchAsJsonAsync(
            $"/api/v1/rentals/{rental.Uuid}/checklists/{checklist.Uuid}/items/{checklist.Items[0].Uuid}",
            new UpdateChecklistItemDto { QuantityChecked = 1, ConditionOk = true });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // =========================================================================
    // Generate DROPOFF
    // =========================================================================

    [Fact]
    public async Task POST_generate_dropoff_without_source_returns_bad_request()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var statusGuid = await RentalTestHelpers.EnsureRentalStatusAsync();
        var (rental, _) = await RentalTestHelpers.SeedRentalWithApprovedItemAsync(admin, statusGuid);

        var response = await admin.AppClient.PostAsJsonAsync(
            $"/api/v1/rentals/{rental.Uuid}/checklists/generate",
            new GenerateChecklistDto
            {
                ChecklistType = ChecklistType.DROPOFF,
                // SourceChecklistGuid intentionally omitted
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_generate_dropoff_mirrors_pickup_items()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var statusGuid = await RentalTestHelpers.EnsureRentalStatusAsync();
        var (rental, rentalItemUuid) = await RentalTestHelpers.SeedRentalWithApprovedItemAsync(admin, statusGuid);
        var pickup = await RentalTestHelpers.GeneratePickupChecklistAsync(admin, rental.Uuid);

        // Generate DROPOFF referencing the PICKUP
        var response = await admin.AppClient.PostAsJsonAsync(
            $"/api/v1/rentals/{rental.Uuid}/checklists/generate",
            new GenerateChecklistDto
            {
                ChecklistType = ChecklistType.DROPOFF,
                SourceChecklistGuid = pickup.Uuid,
                Notes = "Return inspection",
            });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var dropoff = await RentalTestHelpers.DeserializeAsync<ChecklistView>(response);
        dropoff.ChecklistType.Should().Be(ChecklistType.DROPOFF);
        dropoff.SourceChecklistUuid.Should().Be(pickup.Uuid);
        dropoff.TotalItems.Should().Be(pickup.TotalItems);
        dropoff.Items.Should().HaveCount(1);
        dropoff.Items[0].RentalItemUuid.Should().Be(rentalItemUuid);
        dropoff.Items[0].IsChecked.Should().BeFalse();
    }

    [Fact]
    public async Task POST_generate_dropoff_with_non_pickup_source_returns_bad_request()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var statusGuid = await RentalTestHelpers.EnsureRentalStatusAsync();
        var (rental, _) = await RentalTestHelpers.SeedRentalWithApprovedItemAsync(admin, statusGuid);
        var pickup = await RentalTestHelpers.GeneratePickupChecklistAsync(admin, rental.Uuid);

        // Generate a DROPOFF from another DROPOFF (invalid)
        var firstDropoff = await admin.AppClient.PostAsJsonAsync(
            $"/api/v1/rentals/{rental.Uuid}/checklists/generate",
            new GenerateChecklistDto
            {
                ChecklistType = ChecklistType.DROPOFF,
                SourceChecklistGuid = pickup.Uuid,
            });
        firstDropoff.StatusCode.Should().Be(HttpStatusCode.Created);
        var dropoff = await RentalTestHelpers.DeserializeAsync<ChecklistView>(firstDropoff);

        // Now try to use the DROPOFF as a source — must be rejected
        var response = await admin.AppClient.PostAsJsonAsync(
            $"/api/v1/rentals/{rental.Uuid}/checklists/generate",
            new GenerateChecklistDto
            {
                ChecklistType = ChecklistType.DROPOFF,
                SourceChecklistGuid = dropoff.Uuid,
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_generate_dropoff_checklist_appears_in_listing()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var statusGuid = await RentalTestHelpers.EnsureRentalStatusAsync();
        var (rental, _) = await RentalTestHelpers.SeedRentalWithApprovedItemAsync(admin, statusGuid);
        var pickup = await RentalTestHelpers.GeneratePickupChecklistAsync(admin, rental.Uuid);

        _ = await admin.AppClient.PostAsJsonAsync(
            $"/api/v1/rentals/{rental.Uuid}/checklists/generate",
            new GenerateChecklistDto
            {
                ChecklistType = ChecklistType.DROPOFF,
                SourceChecklistGuid = pickup.Uuid,
            });

        var response = await admin.AppClient.GetAsync(
            $"/api/v1/rentals/{rental.Uuid}/checklists");

        var checklists = await RentalTestHelpers.DeserializeAsync<ListViewDto<ChecklistView>>(response);
        checklists.list.Should().HaveCount(2);
        checklists.list.Should().ContainSingle(c => c.ChecklistType == ChecklistType.PICKUP);
        checklists.list.Should().ContainSingle(c => c.ChecklistType == ChecklistType.DROPOFF);
    }
}
