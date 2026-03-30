using LumenForgeServer.Inventory.Domain;
using NodaTime;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Inventory.Dto.View;

/// <summary>
/// View model for devices including related vendor, categories, stock, and parameters.
/// </summary>
public sealed record DeviceView
{
    /// <summary>Unique device identifier.</summary>
    [JsonPropertyName("guid")]
    public Guid Guid { get; init; }

    /// <summary>Device serial number.</summary>
    [JsonPropertyName("serial_number")]
    public required string SerialNumber { get; init; }

    /// <summary>Human-readable device name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>Device description or notes.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>URL pointing to a photo of the device.</summary>
    [JsonPropertyName("photo_url")]
    public string? PhotoUrl { get; init; }

    /// <summary>Purchase price of the device.</summary>
    [JsonPropertyName("purchase_price")]
    public decimal PurchasePrice { get; init; }

    /// <summary>Date the device was purchased.</summary>
    [JsonPropertyName("purchase_date")]
    public DateOnly PurchaseDate { get; init; }

    /// <summary>Unit type used to interpret the stock amount.</summary>
    [JsonPropertyName("stock_unit_type")]
    public StockUnitType StockUnitType { get; init; }

    /// <summary>Quantity available in stock.</summary>
    [JsonPropertyName("stock_amount")]
    public long StockAmount { get; init; }

    /// <summary>Current maintenance status GUID.</summary>
    [JsonPropertyName("maintenance_status_uuid")]
    public Guid MaintenanceStatusUuid { get; init; }

    /// <summary>Display name of the current maintenance status.</summary>
    [JsonPropertyName("maintenance_status_name")]
    public required string MaintenanceStatusName { get; init; }

    /// <summary>Vendor that supplied this device.</summary>
    [JsonPropertyName("vendor")]
    public required VendorView Vendor { get; init; }

    /// <summary>Active stock bindings (reservations) for this device.</summary>
    [JsonPropertyName("stock_bindings")]
    public IReadOnlyList<StockBindingView> StockBindings { get; init; } = [];

    /// <summary>Key/value parameters associated with this device.</summary>
    [JsonPropertyName("parameters")]
    public IReadOnlyList<DeviceParameterView> Parameters { get; init; } = [];

    /// <summary>Categories this device belongs to.</summary>
    [JsonPropertyName("categories")]
    public IReadOnlyList<CategoryView> Categories { get; init; } = [];

    /// <summary>Timestamp when the device was created.</summary>
    [JsonPropertyName("created_at")]
    public Instant CreatedAt { get; init; }

    /// <summary>Timestamp when the device was last updated.</summary>
    [JsonPropertyName("updated_at")]
    public Instant UpdatedAt { get; init; }

    /// <summary>
    /// Executes the from entity operation.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="device">Input value used by this operation.</param>
    /// <returns>The operation result.</returns>
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

        var stockBindings = device.StockBindings
            .Select(StockBindingView.FromEntity)
            .OrderBy(sb => sb.Start)
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
            StockUnitType = device.StockUnitType,
            StockAmount = device.StockAmount,
            MaintenanceStatusUuid = device.MaintenanceStatus.Uuid,
            MaintenanceStatusName = device.MaintenanceStatus.Name,
            Vendor = VendorView.FromEntity(device.Vendor),
            StockBindings = stockBindings,
            Parameters = parameters,
            Categories = categories,
            CreatedAt = device.CreatedAt,
            UpdatedAt = device.UpdatedAt
        };
    }
}
