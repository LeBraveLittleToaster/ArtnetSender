using NodaTime;

namespace LumenForgeServer.Inventory.Domain;

/// <summary>
/// Represents a physical or logical device in inventory.
/// </summary>
public class Device
{
    /// <summary>
    /// Internal database identifier.
    /// </summary>
    public long Id { get; set; }
    /// <summary>
    /// External identifier used for API interactions.
    /// </summary>
    public Guid Guid { get; set; }
    /// <summary>
    /// Device serial number, expected to be unique.
    /// </summary>
    public string SerialNumber { get; set; } = null!;
    /// <summary>
    /// Optional display name for the device.
    /// </summary>
    public string? DeviceName { get; set; }
    /// <summary>
    /// Optional description or notes for the device.
    /// </summary>
    public string? DeviceDescription { get; set; }
    /// <summary>
    /// Optional URL to a device photo.
    /// </summary>
    public string? PhotoUrl { get; set; }

    /// <summary>
    /// Foreign key for the vendor.
    /// </summary>
    public long VendorId { get; set; }
    /// <summary>
    /// Vendor that supplied the device.
    /// </summary>
    public Vendor Vendor { get; set; } = null!;

    /// <summary>
    /// Purchase price for the device.
    /// </summary>
    public decimal PurchasePrice { get; set; }
    /// <summary>
    /// Purchase date for the device.
    /// </summary>
    public DateOnly PurchaseDate { get; set; }

    /// <summary>
    /// Foreign key for the maintenance status.
    /// </summary>
    public long MaintenanceStatusId { get; set; }
    /// <summary>
    /// Current maintenance status for the device.
    /// </summary>
    public MaintenanceStatus MaintenanceStatus { get; set; } = null!;

    /// <summary>
    /// Timestamp when the device record was created.
    /// </summary>
    public Instant CreatedAt { get; set; }
    /// <summary>
    /// Timestamp when the device record was last updated.
    /// </summary>
    public Instant UpdatedAt { get; set; }
    /// <summary>
    /// Type of stock this item is measured in (e.g., units, kilograms, liters).
    /// </summary>
    public StockUnitType StockUnitType { get; set; }
    /// <summary>
    /// Total amount of this item available
    /// </summary>
    public long StockAmount { get; set; }
    /// <summary>
    /// StockBindings the device is associated with.
    /// </summary>
    public List<StockBinding> StockBindings { get; set; } = new();
    /// <summary>
    /// Parameter entries associated with the device.
    /// </summary>
    public List<DeviceParameter> Parameters { get; set; } = new();
    /// <summary>
    /// Category links for the device.
    /// </summary>
    public List<DeviceCategory> DeviceCategories { get; set; } = new();
    /// <summary>
    /// Relations where this device acts as container/parent.
    /// </summary>
    public List<DeviceRelation> ChildDeviceRelations { get; set; } = new();
    /// <summary>
    /// Relations where this device is contained in another device.
    /// </summary>
    public List<DeviceRelation> ParentDeviceRelations { get; set; } = new();
}
