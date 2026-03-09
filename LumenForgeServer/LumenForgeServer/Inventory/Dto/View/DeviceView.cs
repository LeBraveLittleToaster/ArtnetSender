using LumenForgeServer.Inventory.Domain;
using NodaTime;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Inventory.Dto.View;

/// <summary>
/// View model for devices including related vendor, categories, stock, and parameters.
/// </summary>
public sealed record DeviceView
{
    [JsonPropertyName("Guid")]
    public Guid Guid { get; init; }

    [JsonPropertyName("serial_number")]
    public required string SerialNumber { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("photo_url")]
    public string? PhotoUrl { get; init; }

    [JsonPropertyName("purchase_price")]
    public decimal PurchasePrice { get; init; }

    [JsonPropertyName("purchase_date")]
    public DateOnly PurchaseDate { get; init; }

    [JsonPropertyName("maintenance_status_uuid")]
    public Guid MaintenanceStatusUuid { get; init; }

    [JsonPropertyName("maintenance_status_name")]
    public required string MaintenanceStatusName { get; init; }

    [JsonPropertyName("vendor")]
    public required VendorView Vendor { get; init; }

    [JsonPropertyName("stock_bindings")]
    public List<StockBindingView> Stock { get; init; }

    [JsonPropertyName("parameters")]
    public IReadOnlyList<DeviceParameterView> Parameters { get; init; } = [];

    [JsonPropertyName("categories")]
    public IReadOnlyList<CategoryView> Categories { get; init; } = [];

    [JsonPropertyName("created_at")]
    public Instant CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public Instant UpdatedAt { get; init; }

    public static DeviceView FromEntity(Device device)
    {
        var categories = device.DeviceCategories
            .Select(dc => CategoryView.FromEntity(dc.Category))
            .OrderBy(c => c.Name)
            .ToArray();

        var parameters = device.Parameters
            .Select(DeviceParameterView.FromEntity)
            .OrderBy(p => p.Key)
            .ToArray();

        return new DeviceView
        {
            Guid = device.Guid,
            SerialNumber = device.SerialNumber,
            Name = device.DeviceName,
            Description = device.DeviceDescription,
            PhotoUrl = device.PhotoUrl,
            PurchasePrice = device.PurchasePrice,
            PurchaseDate = device.PurchaseDate,
            MaintenanceStatusUuid = device.MaintenanceStatus.Uuid,
            MaintenanceStatusName = device.MaintenanceStatus.Name,
            Vendor = VendorView.FromEntity(device.Vendor),
            Parameters = parameters,
            Categories = categories,
            CreatedAt = device.CreatedAt,
            UpdatedAt = device.UpdatedAt
        };
    }
}
