using FluentAssertions;
using LumenForgeServer.Catalogue.Dto.Command;
using LumenForgeServer.Catalogue.Dto.View;
using LumenForgeServer.Common;
using LumenForgeServer.IntegrationTests.TestSupport;
using LumenForgeServer.Inventory.Domain;
using LumenForgeServer.Inventory.Dto.Create;
using LumenForgeServer.Inventory.Dto.View;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace LumenForgeServer.IntegrationTests.Inventory;

/// <summary>
/// Shared helper methods for inventory integration tests.
/// </summary>
internal static class InventoryTestHelpers
{
    public static async Task<CategoryView> CreateCategoryAsync(TestUserBundle userBundle, string? name = null)
    {
        var categoryName = name ?? $"Category-{Guid.NewGuid()}";
        var response = await userBundle.AppClient.PutAsJsonAsync("/api/v1/inventory/categories", new CreateCategoryDto
        {
            Name = categoryName,
            Description = "Category description " + Guid.NewGuid()
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await DeserializeResponseAsync<CategoryView>(response);
    }

    public static async Task<VendorView> CreateVendorAsync(TestUserBundle userBundle, string? name = null)
    {
        var vendorName = name ?? $"Vendor-{Guid.NewGuid()}";
        var response = await userBundle.AppClient.PutAsJsonAsync("/api/v1/inventory/vendors", new CreateVendorDto
        {
            Name = vendorName
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await DeserializeResponseAsync<VendorView>(response);
    }

    public static async Task<DeviceView> CreateDeviceAsync(
        TestUserBundle userBundle,
        Guid vendorGuid,
        IReadOnlyCollection<Guid>? categoryGuids = null,
        string? serialNumber = null)
    {
        var response = await userBundle.AppClient.PutAsJsonAsync("/api/v1/inventory/devices", new CreateDeviceDto
        {
            SerialNumber = serialNumber ?? $"SN-{Guid.NewGuid()}",
            Name = "Device " + Guid.NewGuid(),
            Description = "Device Description " + Guid.NewGuid(),
            PhotoUrl = "https://example.com/device.jpg",
            VendorGuid = vendorGuid,
            PurchasePrice = 1234.56m,
            PurchaseDate = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            StockAmount = 5,
            StockUnitType = StockUnitType.UNIT,
            Parameters =
            [
                new CreateDeviceParameterDto
                {
                    Key = "manual_url",
                    Value = "https://example.com/manual.pdf"
                }
            ],
            CategoryGuids = categoryGuids?.ToList() ?? []
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await DeserializeResponseAsync<DeviceView>(response);
    }

    public static async Task<StockBindingView> CreateStockBindingAsync(
        TestUserBundle userBundle,
        Guid deviceGuid,
        BindingType bindingType,
        NodaTime.Instant start,
        NodaTime.Instant end)
    {
        var response = await userBundle.AppClient.PutAsJsonAsync($"/api/v1/inventory/devices/{deviceGuid}/stock-bindings",
            new CreateStockBindingDto
            {
                BindingType = bindingType,
                Start = start.ToString(),
                End = end.ToString()
            });

        if (response.StatusCode != HttpStatusCode.Created)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Failed to create stock binding: {response.StatusCode}. Response: {body}");
        }

        return await DeserializeResponseAsync<StockBindingView>(response);
    }

    public static async Task<CatalogueItemView> CreateCatalogueItemAsync(
        TestUserBundle userBundle,
        Guid deviceGuid,
        bool isPublished = true,
        string? name = null)
    {
        var response = await userBundle.AppClient.PutAsJsonAsync("/api/v1/catalogue/items", new CreateCatalogueItemDto
        {
            DeviceGuid = deviceGuid,
            Name = name ?? $"Catalogue Item {Guid.NewGuid()}",
            Description = "Catalogue description " + Guid.NewGuid(),
            PhotoUrl = "https://example.com/catalogue.jpg",
            IsPublished = isPublished,
            SortOrder = 0
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await DeserializeResponseAsync<CatalogueItemView>(response);
    }

    public static async Task<T> DeserializeResponseAsync<T>(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        var payload = JsonSerializer.Deserialize<T>(body, Json.GetJsonSerializerOptions());
        payload.Should().NotBeNull();
        return payload!;
    }
}
