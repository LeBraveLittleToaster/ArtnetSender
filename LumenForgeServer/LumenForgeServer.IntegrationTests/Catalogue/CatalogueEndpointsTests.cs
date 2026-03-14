using FluentAssertions;
using LumenForgeServer.Auth.Domain;
using LumenForgeServer.Auth.Dto.Views;
using LumenForgeServer.Catalogue.Dto.Command;
using LumenForgeServer.Catalogue.Dto.View;
using LumenForgeServer.IntegrationTests.Collections;
using LumenForgeServer.IntegrationTests.Fixtures;
using LumenForgeServer.IntegrationTests.Inventory;
using LumenForgeServer.IntegrationTests.TestSupport;
using LumenForgeServer.Inventory.Dto.View;
using System.Net;
using System.Net.Http.Json;

namespace LumenForgeServer.IntegrationTests.Catalogue;

/// <summary>
/// Integration tests for catalogue endpoints.
/// </summary>
[Collection(AuthCollection.Name)]
public class CatalogueEndpointsTests(AuthFixture fixture)
{
    [Fact]
    public async Task GET_catalogue_items_is_public_and_only_returns_published_items()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var publishedDevice = await CreateCatalogueDeviceAsync(admin);
        var hiddenDevice = await CreateCatalogueDeviceAsync(admin);
        var token = Guid.NewGuid().ToString("N");

        var publishedItem = await InventoryTestHelpers.CreateCatalogueItemAsync(admin, publishedDevice.Guid, true, $"Published Item {token}");
        var hiddenItem = await InventoryTestHelpers.CreateCatalogueItemAsync(admin, hiddenDevice.Guid, false, $"Hidden Item {token}");

        using var client = fixture.GetAnonymousClient();
        var response = await client.GetAsync($"/api/v1/catalogue/items?search={token}&limit=10&offset=0");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var listed = await InventoryTestHelpers.DeserializeResponseAsync<ListViewDto<CatalogueItemView>>(response);
        listed.total.Should().Be(1);
        listed.list.Should().ContainSingle(i => i.Guid == publishedItem.Guid);
        listed.list.Should().NotContain(i => i.Guid == hiddenItem.Guid);
    }

    [Fact]
    public async Task GET_catalogue_item_returns_not_found_for_unpublished_item_without_include_flag()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var device = await CreateCatalogueDeviceAsync(admin);
        var hiddenItem = await InventoryTestHelpers.CreateCatalogueItemAsync(admin, device.Guid, false);

        using var client = fixture.GetAnonymousClient();
        var response = await client.GetAsync($"/api/v1/catalogue/items/{hiddenItem.Guid}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_catalogue_item_include_unpublished_as_anonymous_returns_unauthorized()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var device = await CreateCatalogueDeviceAsync(admin);
        var hiddenItem = await InventoryTestHelpers.CreateCatalogueItemAsync(admin, device.Guid, false);

        using var client = fixture.GetAnonymousClient();
        var response = await client.GetAsync($"/api/v1/catalogue/items/{hiddenItem.Guid}?include_unpublished=true");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_catalogue_item_include_unpublished_as_non_reader_returns_forbidden()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var nonAdmin = await fixture.CreateNewUserAsync(CreateTestUserDto.CreateTestUser());
        var device = await CreateCatalogueDeviceAsync(admin);
        var hiddenItem = await InventoryTestHelpers.CreateCatalogueItemAsync(admin, device.Guid, false);

        var response = await nonAdmin.AppClient.GetAsync($"/api/v1/catalogue/items/{hiddenItem.Guid}?include_unpublished=true");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GET_catalogue_items_include_unpublished_as_catalogue_reader_returns_hidden_items()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var reader = await fixture.CreateNewUserWithRolesAsync(CreateTestUserDto.CreateTestUser(), [Permissions.CatalogueRead]);
        var publishedDevice = await CreateCatalogueDeviceAsync(admin);
        var hiddenDevice = await CreateCatalogueDeviceAsync(admin);
        var token = Guid.NewGuid().ToString("N");

        var publishedItem = await InventoryTestHelpers.CreateCatalogueItemAsync(admin, publishedDevice.Guid, true, $"Reader Published {token}");
        var hiddenItem = await InventoryTestHelpers.CreateCatalogueItemAsync(admin, hiddenDevice.Guid, false, $"Reader Hidden {token}");

        var response = await reader.AppClient.GetAsync($"/api/v1/catalogue/items?publishedOnly=false&search={token}&limit=10&offset=0");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var listed = await InventoryTestHelpers.DeserializeResponseAsync<ListViewDto<CatalogueItemView>>(response);
        listed.total.Should().Be(2);
        listed.list.Should().Contain(i => i.Guid == publishedItem.Guid);
        listed.list.Should().Contain(i => i.Guid == hiddenItem.Guid);
    }

    [Fact]
    public async Task GET_catalogue_items_are_sorted_by_sort_order_then_name()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var deviceA = await CreateCatalogueDeviceAsync(admin);
        var deviceB = await CreateCatalogueDeviceAsync(admin);
        var deviceC = await CreateCatalogueDeviceAsync(admin);

        await admin.AppClient.PutAsJsonAsync("/api/v1/catalogue/items", new CreateCatalogueItemDto
        {
            DeviceGuid = deviceA.Guid,
            Name = "Zulu",
            Description = "Item Z",
            IsPublished = true,
            SortOrder = 2
        });

        await admin.AppClient.PutAsJsonAsync("/api/v1/catalogue/items", new CreateCatalogueItemDto
        {
            DeviceGuid = deviceB.Guid,
            Name = "Bravo",
            Description = "Item B",
            IsPublished = true,
            SortOrder = 1
        });

        await admin.AppClient.PutAsJsonAsync("/api/v1/catalogue/items", new CreateCatalogueItemDto
        {
            DeviceGuid = deviceC.Guid,
            Name = "Alpha",
            Description = "Item A",
            IsPublished = true,
            SortOrder = 1
        });

        using var client = fixture.GetAnonymousClient();
        var response = await client.GetAsync("/api/v1/catalogue/items?published_only=true&limit=10&offset=0");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var listed = await InventoryTestHelpers.DeserializeResponseAsync<ListViewDto<CatalogueItemView>>(response);
        listed.list.Select(i => i.Name).Should().ContainInOrder("Alpha", "Bravo", "Zulu");
    }

    [Fact]
    public async Task PUT_catalogue_item_as_anonymous_returns_unauthorized()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var device = await CreateCatalogueDeviceAsync(admin);
        using var client = fixture.GetAnonymousClient();

        var response = await client.PutAsJsonAsync("/api/v1/catalogue/items", new CreateCatalogueItemDto
        {
            DeviceGuid = device.Guid,
            Name = "Anonymous item",
            Description = "Should fail",
            IsPublished = true,
            SortOrder = 0
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PUT_catalogue_item_as_non_admin_returns_forbidden()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var nonAdmin = await fixture.CreateNewUserAsync(CreateTestUserDto.CreateTestUser());
        var device = await CreateCatalogueDeviceAsync(admin);

        var response = await nonAdmin.AppClient.PutAsJsonAsync("/api/v1/catalogue/items", new CreateCatalogueItemDto
        {
            DeviceGuid = device.Guid,
            Name = "NoAdminCatalogue-" + Guid.NewGuid(),
            Description = "Should fail",
            IsPublished = true,
            SortOrder = 0
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Catalogue_item_crud_flow_works_for_admin()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var device = await CreateCatalogueDeviceAsync(admin);

        var createResponse = await admin.AppClient.PutAsJsonAsync("/api/v1/catalogue/items", new CreateCatalogueItemDto
        {
            DeviceGuid = device.Guid,
            Name = "Catalogue-" + Guid.NewGuid(),
            Description = "Initial description",
            PhotoUrl = "https://example.com/item.jpg",
            IsPublished = true,
            SortOrder = 1
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await InventoryTestHelpers.DeserializeResponseAsync<CatalogueItemView>(createResponse);

        var getResponse = await admin.AppClient.GetAsync($"/api/v1/catalogue/items/{created.Guid}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var listResponse = await admin.AppClient.GetAsync($"/api/v1/catalogue/items?search={Uri.EscapeDataString(created.Name)}&limit=10&offset=0");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var listed = await InventoryTestHelpers.DeserializeResponseAsync<ListViewDto<CatalogueItemView>>(listResponse);
        listed.list.Should().Contain(i => i.Guid == created.Guid);

        var patchResponse = await admin.AppClient.PatchAsJsonAsync($"/api/v1/catalogue/items/{created.Guid}", new UpdateCatalogueItemDto
        {
            Name = created.Name + "-Updated",
            Description = "Updated description",
            IsPublished = false,
            SortOrder = 2
        });
        patchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await InventoryTestHelpers.DeserializeResponseAsync<CatalogueItemView>(patchResponse);
        updated.Name.Should().EndWith("-Updated");
        updated.Description.Should().Be("Updated description");
        updated.IsPublished.Should().BeFalse();
        updated.SortOrder.Should().Be(2);

        var hiddenGetResponse = await admin.AppClient.GetAsync($"/api/v1/catalogue/items/{created.Guid}?include_unpublished=true");
        hiddenGetResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleteResponse = await admin.AppClient.DeleteAsync($"/api/v1/catalogue/items/{created.Guid}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getDeletedResponse = await admin.AppClient.GetAsync($"/api/v1/catalogue/items/{created.Guid}?include_unpublished=true");
        getDeletedResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PUT_catalogue_item_with_duplicate_device_returns_conflict()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var device = await CreateCatalogueDeviceAsync(admin);

        var firstCreate = await admin.AppClient.PutAsJsonAsync("/api/v1/catalogue/items", new CreateCatalogueItemDto
        {
            DeviceGuid = device.Guid,
            Name = "DuplicateDevice-1",
            Description = "Description A",
            IsPublished = true,
            SortOrder = 0
        });
        firstCreate.StatusCode.Should().Be(HttpStatusCode.Created);

        var secondCreate = await admin.AppClient.PutAsJsonAsync("/api/v1/catalogue/items", new CreateCatalogueItemDto
        {
            DeviceGuid = device.Guid,
            Name = "DuplicateDevice-2",
            Description = "Description B",
            IsPublished = true,
            SortOrder = 0
        });

        secondCreate.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task PUT_catalogue_item_with_empty_device_guid_returns_bad_request()
    {
        var admin = await fixture.GetInitialAdminUserAsync();

        var response = await admin.AppClient.PutAsJsonAsync("/api/v1/catalogue/items", new CreateCatalogueItemDto
        {
            DeviceGuid = Guid.Empty,
            Name = "Invalid Device Guid",
            Description = "Should fail",
            IsPublished = true,
            SortOrder = 0
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PUT_catalogue_item_with_unknown_device_returns_not_found()
    {
        var admin = await fixture.GetInitialAdminUserAsync();

        var response = await admin.AppClient.PutAsJsonAsync("/api/v1/catalogue/items", new CreateCatalogueItemDto
        {
            DeviceGuid = Guid.NewGuid(),
            Name = "Unknown Device",
            Description = "Should fail",
            IsPublished = true,
            SortOrder = 0
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PUT_catalogue_item_with_invalid_payload_returns_bad_request()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var device = await CreateCatalogueDeviceAsync(admin);

        var response = await admin.AppClient.PutAsJsonAsync("/api/v1/catalogue/items", new CreateCatalogueItemDto
        {
            DeviceGuid = device.Guid,
            Name = "   ",
            Description = "Valid description",
            IsPublished = true,
            SortOrder = -1
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PATCH_catalogue_item_as_non_admin_returns_forbidden()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var nonAdmin = await fixture.CreateNewUserAsync(CreateTestUserDto.CreateTestUser());
        var device = await CreateCatalogueDeviceAsync(admin);
        var item = await InventoryTestHelpers.CreateCatalogueItemAsync(admin, device.Guid);

        var response = await nonAdmin.AppClient.PatchAsJsonAsync($"/api/v1/catalogue/items/{item.Guid}", new UpdateCatalogueItemDto
        {
            Name = "Blocked update"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PATCH_catalogue_item_with_unknown_guid_returns_not_found()
    {
        var admin = await fixture.GetInitialAdminUserAsync();

        var response = await admin.AppClient.PatchAsJsonAsync($"/api/v1/catalogue/items/{Guid.NewGuid()}", new UpdateCatalogueItemDto
        {
            Name = "Missing"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PATCH_catalogue_item_with_empty_device_guid_returns_bad_request()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var device = await CreateCatalogueDeviceAsync(admin);
        var item = await InventoryTestHelpers.CreateCatalogueItemAsync(admin, device.Guid);

        var response = await admin.AppClient.PatchAsJsonAsync($"/api/v1/catalogue/items/{item.Guid}", new UpdateCatalogueItemDto
        {
            DeviceGuid = Guid.Empty
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DELETE_catalogue_item_as_non_admin_returns_forbidden()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var nonAdmin = await fixture.CreateNewUserAsync(CreateTestUserDto.CreateTestUser());
        var device = await CreateCatalogueDeviceAsync(admin);
        var item = await InventoryTestHelpers.CreateCatalogueItemAsync(admin, device.Guid);

        var response = await nonAdmin.AppClient.DeleteAsync($"/api/v1/catalogue/items/{item.Guid}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GET_catalogue_item_not_found_returns_not_found()
    {
        using var client = fixture.GetAnonymousClient();

        var response = await client.GetAsync($"/api/v1/catalogue/items/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_catalogue_items_invalid_limit_returns_bad_request()
    {
        using var client = fixture.GetAnonymousClient();

        var response = await client.GetAsync("/api/v1/catalogue/items?limit=0&offset=0");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_catalogue_items_negative_offset_returns_bad_request()
    {
        using var client = fixture.GetAnonymousClient();

        var response = await client.GetAsync("/api/v1/catalogue/items?limit=10&offset=-1");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static async Task<DeviceView> CreateCatalogueDeviceAsync(TestUserBundle admin)
    {
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var category = await InventoryTestHelpers.CreateCategoryAsync(admin);
        return await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid, [category.Guid]);
    }
}