using LumenForgeServer.Common;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Inventory.Dto.Create;

/// <summary>
/// Payload for creating a device along with stock, parameters, and category links.
/// </summary>
public record CreateDeviceDto
{
    /// <summary>
    /// Device serial number, expected to be unique.
    /// </summary>
    [Required]
    [StringLength(256, MinimumLength = 1)]
    [RegularExpression(@".*\S.*")]
    [JsonPropertyName("serialNumber")]
    public required string SerialNumber { get; set; }
    /// <summary>
    /// Human-readable device name.
    /// </summary>
    [StringLength(512)]
    [JsonPropertyName("name")]
    public required string? Name { get; set; }
    /// <summary>
    /// Device description or notes.
    /// </summary>
    [StringLength(4000)]
    [JsonPropertyName("description")]
    public required string? Description { get; set; }
    /// <summary>
    /// URL pointing to a photo of the device.
    /// </summary>
    [StringLength(2000)]
    [Url]
    [JsonPropertyName("photoUrl")]
    public string? PhotoUrl { get; set; }
    /// <summary>
    /// Vendor UUID to associate with the device.
    /// </summary>
    [JsonPropertyName("vendorGuid")]
    public required Guid VendorGuid { get; set; }
    /// <summary>
    /// Optional maintenance status UUID; defaults to first available status when omitted.
    /// </summary>
    [JsonPropertyName("maintenanceStatusUuid")]
    public Guid? MaintenanceStatusUuid { get; set; }
    /// <summary>
    /// Purchase price of the device.
    /// </summary>
    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    [JsonPropertyName("purchasePrice")]
    public decimal PurchasePrice { get; set; }
    /// <summary>
    /// Purchase date of the device.
    /// </summary>
    [JsonPropertyName("purchaseDate")]
    public DateOnly PurchaseDate { get; set; }
    /// <summary>
    /// Unit type used to interpret the stock amount.
    /// </summary>
    [JsonPropertyName("stockUnitType")]
    public StockUnitType StockUnitType { get; set; }
    /// <summary>
    /// Quantity available in stock.
    /// </summary>
    [Range(0, long.MaxValue)]
    [JsonPropertyName("stockAmount")]
    public long StockAmount { get; set; }
    /// <summary>
    /// Device parameter key/value pairs.
    /// </summary>
    [JsonPropertyName("parameters")]
    public List<CreateDeviceParameterDto> Parameters { get; set; } = [];
    /// <summary>
    /// Category UUIDs to associate with the device.
    /// </summary>
    [JsonPropertyName("categoryGuids")]
    public List<Guid> CategoryGuids { get; set; } = [];
}
