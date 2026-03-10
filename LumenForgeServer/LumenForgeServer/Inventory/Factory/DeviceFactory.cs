using LumenForgeServer.Inventory.Domain;
using LumenForgeServer.Inventory.Dto.Create;
using NodaTime;

namespace LumenForgeServer.Inventory.Factory;

/// <summary>
/// Factory methods for creating device aggregates.
/// </summary>
public static class DeviceFactory
{
    /// <summary>
    /// Creates a device aggregate from a creation payload and resolved ids.
    /// </summary>
    /// <param name="dto">Payload containing device details.</param>
    /// <param name="vendorId">Resolved internal vendor id.</param>
    /// <param name="maintenanceStatusId">Resolved maintenance status id.</param>
    /// <param name="categoryIds">Resolved internal category ids.</param>
    /// <returns>A new <see cref="Device"/> aggregate with stock, parameters, and categories.</returns>
    internal static Device Create(CreateDeviceDto dto, long vendorId, long maintenanceStatusId, IReadOnlyList<long> categoryIds)
    {
        var now = SystemClock.Instance.GetCurrentInstant();
        return new Device
        {
            SerialNumber = dto.SerialNumber,
            DeviceName = dto.Name,
            DeviceDescription = dto.Description,
            PhotoUrl = dto.PhotoUrl,
            VendorId = vendorId,
            MaintenanceStatusId = maintenanceStatusId,
            PurchasePrice = dto.PurchasePrice,
            PurchaseDate = dto.PurchaseDate,
            StockUnitType = dto.StockUnitType,
            StockAmount = dto.StockAmount,
            Guid = Guid.CreateVersion7(),
            CreatedAt = now,
            UpdatedAt = now,
            Parameters = dto.Parameters.Select(p => new DeviceParameter
            {
                ParamKey = p.Key,
                Value = p.Value,
                CreatedAt = now,
                UpdatedAt = now
            }).ToList(),
            DeviceCategories = categoryIds.Select(id => new DeviceCategory
            {
                CategoryId = id
            }).ToList()
        };
    }
}
