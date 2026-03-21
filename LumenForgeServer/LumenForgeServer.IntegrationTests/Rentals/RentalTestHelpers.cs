#if false // Disabled: references removed Dto.View types from pre-rework rental API
using FluentAssertions;
using LumenForgeServer.Common;
using LumenForgeServer.IntegrationTests.Fixtures;
using LumenForgeServer.IntegrationTests.Inventory;
using LumenForgeServer.IntegrationTests.TestSupport;
using LumenForgeServer.Inventory.Domain;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Dto.Command;
using LumenForgeServer.Rentals.Dto.View;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace LumenForgeServer.IntegrationTests.Rentals;

/// <summary>
/// Shared helper methods for rental integration tests.
/// </summary>
internal static class RentalTestHelpers
{
    private static readonly AppDbFixture DbFixture = new();

    public static string GetChecklistsPath(Guid rentalGuid)
        => $"/api/v1/rentals/{rentalGuid}/checklists";

    public static string GetChecklistGeneratePath(Guid rentalGuid)
        => $"{GetChecklistsPath(rentalGuid)}/generate";

    public static string GetChecklistPath(Guid rentalGuid, Guid checklistGuid)
        => $"{GetChecklistsPath(rentalGuid)}/{checklistGuid}";

    public static string GetChecklistItemPath(Guid rentalGuid, Guid checklistGuid, Guid itemGuid)
        => $"{GetChecklistPath(rentalGuid, checklistGuid)}/items/{itemGuid}";

    public static string GetChecklistSignPath(Guid rentalGuid, Guid checklistGuid)
        => $"{GetChecklistPath(rentalGuid, checklistGuid)}/sign";

    public static string GetChecklistScanPath(Guid rentalGuid, Guid checklistGuid, Guid deviceGuid)
        => $"{GetChecklistPath(rentalGuid, checklistGuid)}/scan?device_guid={deviceGuid}";

    public static Task<RentalStatus> EnsureRentalStatusByNameAsync(string name)
    {
        return Task.FromResult(name.Trim() switch
        {
            "Requested" => RentalStatus.Requested,
            "Approved" => RentalStatus.Approved,
            "Rejected" => RentalStatus.Rejected,
            "PickedUp" => RentalStatus.PickedUp,
            "Returned" => RentalStatus.Returned,
            "Completed" => RentalStatus.Completed,
            "Cancelled" => RentalStatus.Cancelled,
            "Scrapped" => RentalStatus.Scrapped,
            _ => throw new ArgumentOutOfRangeException(nameof(name), $"Unsupported rental status '{name}'.")
        });
    }

    public static Task<RentalStatus> EnsureRentalStatusAsync()
        => Task.FromResult(RentalStatus.Requested);

    /// <summary>
    /// Creates a rental via the API and returns the response view.
    /// </summary>
    public static async Task<RentalView> CreateRentalAsync(
        TestUserBundle user,
        RentalStatus _,
        string? title = null)
    {
        var response = await user.AppClient.PutAsJsonAsync("/api/v1/rentals", new CreateRentalDto
        {
            RequestTitle = title ?? $"Rental-{Guid.NewGuid():N}",
            EventName = "Integration test event",
            Priority = RentalPriority.NORMAL,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await DeserializeAsync<RentalView>(response);
    }

    /// <summary>
    /// Creates a rental via the API then inserts one APPROVED <see cref="RentalItem"/> directly
    /// into the database (no RentalItem endpoint exists yet).
    /// </summary>
    public static async Task<(RentalView Rental, Guid RentalItemUuid)> SeedRentalWithApprovedItemAsync(
        TestUserBundle user,
        RentalStatus rentalStatus)
    {
        var rental = await CreateRentalAsync(user, rentalStatus);

        await using var db = DbFixture.CreateDbContext();
        var rentalEntity = await db.Rentals.SingleAsync(r => r.Uuid == rental.Uuid);

        var now = SystemClock.Instance.GetCurrentInstant();
        var itemUuid = Guid.CreateVersion7();

        db.RentalItems.Add(new RentalItem
        {
            Uuid = itemUuid,
            RentalId = rentalEntity.Id,
            Status = RentalItemStatus.APPROVED,
            QuantityRequested = 2,
            QuantityApproved = 2,
            IsApproved = true,
            ApprovedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
        });

        await db.SaveChangesAsync();
        return (rental, itemUuid);
    }

    /// <summary>
    /// Calls the generate-checklist endpoint with <see cref="ChecklistType.PICKUP"/> and
    /// asserts the response is 201 Created.
    /// </summary>
    public static async Task<ChecklistView> GeneratePickupChecklistAsync(
        TestUserBundle user,
        Guid rentalGuid)
    {
        var response = await user.AppClient.PostAsJsonAsync(
            GetChecklistGeneratePath(rentalGuid),
            new GenerateChecklistDto { ChecklistType = ChecklistType.PICKUP });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await DeserializeAsync<ChecklistView>(response);
    }

    public static async Task<T> DeserializeAsync<T>(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        var payload = JsonSerializer.Deserialize<T>(body, Json.GetJsonSerializerOptions());
        payload.Should().NotBeNull();
        return payload!;
    }

    /// <summary>
    /// Creates a rental with an approved <see cref="RentalItem"/> that is linked to a
    /// <see cref="StockBinding"/> for a freshly created device.
    /// Needed for QR-scan tests where the lookup navigates
    /// <c>ChecklistItem → RentalItem → StockBindings → Device</c>.
    /// </summary>
    public static async Task<(RentalView Rental, Guid RentalItemUuid, Guid DeviceGuid)>
        SeedRentalWithApprovedItemAndDeviceAsync(
            TestUserBundle user,
            RentalStatus rentalStatus,
            Guid vendorGuid)
    {
        var rental = await CreateRentalAsync(user, rentalStatus);
        var device = await InventoryTestHelpers.CreateDeviceAsync(user, vendorGuid);

        // Create a RENTAL_REQUEST stock binding that covers the next 7 days
        var start = SystemClock.Instance.GetCurrentInstant();
        var end = start + Duration.FromDays(7);
        var sbView = await InventoryTestHelpers.CreateStockBindingAsync(
            user, device.Guid, BindingType.RENTAL_REQUEST, start, end);

        await using var db = DbFixture.CreateDbContext();
        var rentalEntity = await db.Rentals.SingleAsync(r => r.Uuid == rental.Uuid);
        var now = SystemClock.Instance.GetCurrentInstant();
        var itemUuid = Guid.CreateVersion7();

        // Insert the approved rental item
        db.RentalItems.Add(new RentalItem
        {
            Uuid = itemUuid,
            RentalId = rentalEntity.Id,
            Status = RentalItemStatus.APPROVED,
            QuantityRequested = 1,
            QuantityApproved = 1,
            IsApproved = true,
            ApprovedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
        });
        await db.SaveChangesAsync();

        // Link the stock binding to the rental item via the join table
        var stockBinding = await db.StockBindings.SingleAsync(sb => sb.Guid == sbView.Guid);
        var rentalItem = await db.RentalItems
            .Include(ri => ri.StockBindings)
            .SingleAsync(ri => ri.Uuid == itemUuid);

        rentalItem.StockBindings.Add(stockBinding);
        await db.SaveChangesAsync();

        return (rental, itemUuid, device.Guid);
    }
}
#endif
