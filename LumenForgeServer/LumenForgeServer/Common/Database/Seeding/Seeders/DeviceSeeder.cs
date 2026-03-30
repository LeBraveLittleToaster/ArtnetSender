using LumenForgeServer.Inventory.Dto.Create;
using LumenForgeServer.Inventory.Service;
using Microsoft.EntityFrameworkCore;

namespace LumenForgeServer.Common.Database.Seeding.Seeders;

/// <summary>Seeds real-world device models loaded from the embedded devices CSV. Dev only.</summary>
public class DeviceSeeder(DeviceService deviceService, AppDbContext db, ILogger<DeviceSeeder> logger) : IDataSeeder
{
    public int Order => 30;
    public SeedEnvironment Environment => SeedEnvironment.Dev;

    /// <summary>
    /// Executes the seed async operation.
    /// </summary>
    /// <remarks>Potential side effects: may modify state as part of this operation.</remarks>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SeedAsync(CancellationToken ct)
    {
        var vendorIndex = await db.Vendors
            .AsNoTracking()
            .ToDictionaryAsync(v => v.Name, v => v.Guid, ct);

        foreach (var row in SeedDataLoader.Load("devices.csv"))
        {
            if (row.Length < 4) continue;

            var name        = row[0].Trim();
            var description = row[1].Trim();
            var serial      = row[2].Trim();
            var vendorName  = row[3].Trim();

            if (!vendorIndex.TryGetValue(vendorName, out var vendorGuid))
            {
                logger.LogWarning("Skipping device '{Name}': vendor '{Vendor}' not found.", name, vendorName);
                continue;
            }

            await deviceService.CreateDevice(new CreateDeviceDto
            {
                VendorGuid   = vendorGuid,
                Name         = name,
                Description  = description,
                SerialNumber = serial,
            }, ct);
        }
    }
}
