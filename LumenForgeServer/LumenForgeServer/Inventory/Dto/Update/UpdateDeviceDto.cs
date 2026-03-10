using LumenForgeServer.Common;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Inventory.Dto.Update;

/// <summary>
/// Payload for updating an existing device.
/// </summary>
public sealed class UpdateDeviceDto : IValidatableObject
{
    /// <summary>
    /// Updated serial number.
    /// </summary>
    [StringLength(256, MinimumLength = 1)]
    [RegularExpression(@".*\S.*")]
    [JsonPropertyName("serialNumber")]
    public string? SerialNumber { get; init; }

    /// <summary>
    /// Updated display name.
    /// </summary>
    [StringLength(512)]
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>
    /// Updated description.
    /// </summary>
    [StringLength(4000)]
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>
    /// Updated photo URL.
    /// </summary>
    [StringLength(2000)]
    [Url]
    [JsonPropertyName("photoUrl")]
    public string? PhotoUrl { get; init; }

    /// <summary>
    /// Updated vendor GUID.
    /// </summary>
    [JsonPropertyName("vendorGuid")]
    public Guid? VendorGuid { get; init; }

    /// <summary>
    /// Updated maintenance status GUID.
    /// </summary>
    [JsonPropertyName("maintenanceStatusUuid")]
    public Guid? MaintenanceStatusUuid { get; init; }

    /// <summary>
    /// Updated purchase price.
    /// </summary>
    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    [JsonPropertyName("purchasePrice")]
    public decimal? PurchasePrice { get; init; }

    /// <summary>
    /// Updated purchase date.
    /// </summary>
    [JsonPropertyName("purchaseDate")]
    public DateOnly? PurchaseDate { get; init; }

    /// <summary>
    /// Updated stock unit type.
    /// </summary>
    [JsonPropertyName("stockUnitType")]
    public StockUnitType? StockUnitType { get; init; }

    /// <summary>
    /// Updated stock amount.
    /// </summary>
    [Range(0, long.MaxValue)]
    [JsonPropertyName("stockAmount")]
    public long? StockAmount { get; init; }

    /// <summary>
    /// Ensures at least one field is provided.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (SerialNumber is null &&
            Name is null &&
            Description is null &&
            PhotoUrl is null &&
            VendorGuid is null &&
            MaintenanceStatusUuid is null &&
            PurchasePrice is null &&
            PurchaseDate is null &&
            StockUnitType is null &&
            StockAmount is null)
        {
            yield return new ValidationResult(
                "At least one updatable field must be provided.",
                new[]
                {
                    nameof(SerialNumber),
                    nameof(Name),
                    nameof(Description),
                    nameof(PhotoUrl),
                    nameof(VendorGuid),
                    nameof(MaintenanceStatusUuid),
                    nameof(PurchasePrice),
                    nameof(PurchaseDate),
                    nameof(StockUnitType),
                    nameof(StockAmount)
                });
        }
    }
}
