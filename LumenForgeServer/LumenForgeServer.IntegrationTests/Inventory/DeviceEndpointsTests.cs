// DeviceEndpointsTests.cs
using FluentAssertions;
using LumenForgeServer.Auth.Dto.Views;
using LumenForgeServer.Common;
using LumenForgeServer.IntegrationTests.Collections;
using LumenForgeServer.IntegrationTests.Fixtures;
using LumenForgeServer.Inventory.Dto.Create;
using LumenForgeServer.Inventory.Dto.Update;
using LumenForgeServer.Inventory.Dto.View;
using System.Net;
using System.Net.Http.Json;

namespace LumenForgeServer.IntegrationTests.Inventory;

/// <summary>
/// Integration tests for device endpoints including stock, categories, and parameters.
/// </summary>
[Collection(AuthCollection.Name)]
public class DeviceEndpointsTests(AuthFixture fixture)
{
    [Fact]
    public async Task GET_devices_requires_authentication()
    {
        using var client = fixture.GetAnonymousClient();

        var response = await client.GetAsync("/api/v1/inventory/devices");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PUT_device_creates_with_vendor_categories_stock_and_parameters()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var categoryA = await InventoryTestHelpers.CreateCategoryAsync(admin);
        var categoryB = await InventoryTestHelpers.CreateCategoryAsync(admin);

        var device = await InventoryTestHelpers.CreateDeviceAsync(
            admin,
            vendor.Guid,
            [categoryA.Guid, categoryB.Guid],
            serialNumber: "SERIAL-" + Guid.NewGuid());

        device.Guid.Should().NotBe(Guid.Empty);
        device.Vendor.Guid.Should().Be(vendor.Guid);
        device.Categories.Select(c => c.Guid).Should().Contain([categoryA.Guid, categoryB.Guid]);
        device.StockAmount.Should().Be(5);
        device.StockUnitType.Should().Be(StockUnitType.UNIT);
        device.StockBindings.Should().BeEmpty();
        device.Parameters.Should().Contain(p => p.Key == "manual_url");

        var getResponse = await admin.AppClient.GetAsync($"/api/v1/inventory/devices/{device.Guid}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PUT_device_with_unknown_vendor_returns_not_found()
    {
        var admin = await fixture.GetInitialAdminUserAsync();

        var response = await admin.AppClient.PutAsJsonAsync("/api/v1/inventory/devices", new CreateDeviceDto
        {
            SerialNumber = "SN-" + Guid.NewGuid(),
            Name = "Device Name",
            Description = "Device Description",
            VendorGuid = Guid.NewGuid(),
            PurchasePrice = 500m,
            PurchaseDate = DateOnly.FromDateTime(DateTime.UtcNow),
            StockAmount = 2,
            StockUnitType = StockUnitType.UNIT,
            Parameters = [],
            CategoryGuids = []
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PUT_device_with_unknown_category_returns_not_found()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);

        var response = await admin.AppClient.PutAsJsonAsync("/api/v1/inventory/devices", new CreateDeviceDto
        {
            SerialNumber = "SN-" + Guid.NewGuid(),
            Name = "Device Name",
            Description = "Device Description",
            VendorGuid = vendor.Guid,
            PurchasePrice = 500m,
            PurchaseDate = DateOnly.FromDateTime(DateTime.UtcNow),
            StockAmount = 2,
            StockUnitType = StockUnitType.UNIT,
            Parameters = [],
            CategoryGuids = [Guid.NewGuid()]
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PUT_device_invalid_payload_returns_bad_request()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);

        var response = await admin.AppClient.PutAsJsonAsync("/api/v1/inventory/devices", new CreateDeviceDto
        {
            SerialNumber = " ",
            Name = "Device Name",
            Description = "Device Description",
            VendorGuid = vendor.Guid,
            PurchasePrice = 100,
            PurchaseDate = DateOnly.FromDateTime(DateTime.UtcNow),
            StockAmount = -1,
            StockUnitType = StockUnitType.UNIT,
            Parameters = [],
            CategoryGuids = []
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_devices_supports_search_and_paging()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var serial = "LOOKUP-" + Guid.NewGuid();
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid, serialNumber: serial);

        var response = await admin.AppClient.GetAsync(
            $"/api/v1/inventory/devices?search={Uri.EscapeDataString(serial)}&limit=10&offset=0");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var listed = await InventoryTestHelpers.DeserializeResponseAsync<ListViewDto<DeviceView>>(response);
        listed.list.Should().Contain(d => d.Guid == device.Guid);
    }

    [Fact]
    public async Task GET_devices_invalid_limit_returns_bad_request()
    {
        var admin = await fixture.GetInitialAdminUserAsync();

        var response = await admin.AppClient.GetAsync("/api/v1/inventory/devices?limit=0&offset=0");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_device_not_found_returns_not_found()
    {
        var admin = await fixture.GetInitialAdminUserAsync();

        var response = await admin.AppClient.GetAsync($"/api/v1/inventory/devices/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PATCH_device_updates_fields_and_vendor()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendorA = await InventoryTestHelpers.CreateVendorAsync(admin);
        var vendorB = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendorA.Guid);

        var patchResponse = await admin.AppClient.PatchAsJsonAsync($"/api/v1/inventory/devices/{device.Guid}", new UpdateDeviceDto
        {
            SerialNumber = "UPDATED-" + Guid.NewGuid(),
            Name = "Updated Device Name",
            Description = "Updated Device Description",
            PhotoUrl = "https://example.com/updated.jpg",
            VendorGuid = vendorB.Guid,
            PurchasePrice = 999.99m,
            PurchaseDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10))
        });

        patchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await InventoryTestHelpers.DeserializeResponseAsync<DeviceView>(patchResponse);
        updated.Vendor.Guid.Should().Be(vendorB.Guid);
        updated.Name.Should().Be("Updated Device Name");
        updated.Description.Should().Be("Updated Device Description");
        updated.PhotoUrl.Should().Be("https://example.com/updated.jpg");
        updated.PurchasePrice.Should().Be(999.99m);
        updated.SerialNumber.Should().StartWith("UPDATED-");
    }

    [Fact]
    public async Task PATCH_device_empty_payload_returns_bad_request()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);

        var response = await admin.AppClient.PatchAsJsonAsync($"/api/v1/inventory/devices/{device.Guid}", new { });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PUT_device_categories_replaces_assignments()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var categoryA = await InventoryTestHelpers.CreateCategoryAsync(admin);
        var categoryB = await InventoryTestHelpers.CreateCategoryAsync(admin);
        var categoryC = await InventoryTestHelpers.CreateCategoryAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid, [categoryA.Guid, categoryB.Guid]);

        var response = await admin.AppClient.PutAsJsonAsync($"/api/v1/inventory/devices/{device.Guid}/categories", new SetDeviceCategoriesDto
        {
            CategoryGuids = [categoryC.Guid]
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await InventoryTestHelpers.DeserializeResponseAsync<DeviceView>(response);
        updated.Categories.Select(c => c.Guid).Should().Equal([categoryC.Guid]);
    }

    [Fact]
    public async Task PUT_device_categories_with_unknown_category_returns_not_found()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);

        var response = await admin.AppClient.PutAsJsonAsync($"/api/v1/inventory/devices/{device.Guid}/categories", new SetDeviceCategoriesDto
        {
            CategoryGuids = [Guid.NewGuid()]
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PUT_device_parameter_upsert_creates_and_updates_value()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);

        var createParamResponse = await admin.AppClient.PutAsJsonAsync($"/api/v1/inventory/devices/{device.Guid}/parameters", new UpsertDeviceParameterDto
        {
            Key = "support_url",
            Value = "https://example.com/support-v1"
        });
        createParamResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updateParamResponse = await admin.AppClient.PutAsJsonAsync($"/api/v1/inventory/devices/{device.Guid}/parameters", new UpsertDeviceParameterDto
        {
            Key = "support_url",
            Value = "https://example.com/support-v2"
        });
        updateParamResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedParameter = await InventoryTestHelpers.DeserializeResponseAsync<DeviceParameterView>(updateParamResponse);
        updatedParameter.Key.Should().Be("support_url");
        updatedParameter.Value.Should().Be("https://example.com/support-v2");

        var getResponse = await admin.AppClient.GetAsync($"/api/v1/inventory/devices/{device.Guid}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshed = await InventoryTestHelpers.DeserializeResponseAsync<DeviceView>(getResponse);
        refreshed.Parameters.Count(p => p.Key == "support_url").Should().Be(1);
        refreshed.Parameters.Should().Contain(p => p.Key == "support_url" && p.Value == "https://example.com/support-v2");
    }

    [Fact]
    public async Task DELETE_device_parameter_removes_and_repeated_delete_returns_not_found()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);

        var firstDelete = await admin.AppClient.DeleteAsync($"/api/v1/inventory/devices/{device.Guid}/parameters/manual_url");
        firstDelete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var secondDelete = await admin.AppClient.DeleteAsync($"/api/v1/inventory/devices/{device.Guid}/parameters/manual_url");
        secondDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PATCH_device_updates_stock_fields_and_rejects_negative_stock_amount()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);

        var updateResponse = await admin.AppClient.PatchAsJsonAsync($"/api/v1/inventory/devices/{device.Guid}", new UpdateDeviceDto
        {
            StockAmount = 42,
            StockUnitType = StockUnitType.KG
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedDevice = await InventoryTestHelpers.DeserializeResponseAsync<DeviceView>(updateResponse);
        updatedDevice.StockAmount.Should().Be(42);
        updatedDevice.StockUnitType.Should().Be(StockUnitType.KG);

        var invalidResponse = await admin.AppClient.PatchAsJsonAsync($"/api/v1/inventory/devices/{device.Guid}", new UpdateDeviceDto
        {
            StockAmount = -5
        });

        invalidResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DELETE_device_removes_record()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var vendor = await InventoryTestHelpers.CreateVendorAsync(admin);
        var device = await InventoryTestHelpers.CreateDeviceAsync(admin, vendor.Guid);

        var deleteResponse = await admin.AppClient.DeleteAsync($"/api/v1/inventory/devices/{device.Guid}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getDeletedResponse = await admin.AppClient.GetAsync($"/api/v1/inventory/devices/{device.Guid}");
        getDeletedResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
