using LumenForgeServer.Catalogue.Dto.Command;
using LumenForgeServer.Catalogue.Service;
using Microsoft.EntityFrameworkCore;

namespace LumenForgeServer.Common.Database.Seeding.Seeders;

/// <summary>Seeds catalogue listings for the devices created by <see cref="DeviceSeeder"/>. Dev only.</summary>
public class CatalogueSeeder(CatalogueService catalogueService, AppDbContext db) : IDataSeeder
{
    public int Order => 40;
    public SeedEnvironment Environment => SeedEnvironment.Dev;

    /// <summary>
    /// Seed definitions mapped to devices by index order.
    /// </summary>
    private static readonly (int DeviceIndex, string Name, string Description, string? PhotoUrl, bool IsPublished, int SortOrder)[] CatalogueDefs =
    [
        (0, "LED Strip Controller",       "Control any addressable RGB strip from a single compact unit. Supports Wi-Fi and direct DMX input.",             "https://placehold.co/600x400", true,  0),
        (1, "RGBW Ceiling Light",         "Smooth, silent RGBW colour mixing for professional stage and architectural environments.",                        "https://placehold.co/600x400", true,  1),
        (2, "DMX Bridge",                 "Plug-and-play USB to DMX512 adapter. Works with all major lighting console software on Windows, macOS and Linux.", null,                           true,  2),
        (3, "Wireless Dimmer",            "Flicker-free trailing-edge dimming over 0–10 V with sub-millisecond response time.",                             "https://placehold.co/600x400", true,  3),
        (4, "Moving Head Spotlight",      "Precision motorised yoke with 190° pan / 120° tilt, replaceable gobos and a 13-colour wheel.",                  "https://placehold.co/600x400", true,  4),
        (5, "LED PAR Can 64",             "Budget-friendly 64-format PAR can loaded with 18 full-colour RGB LEDs and a wide beam angle.",                   null,                           true,  5),
        (6, "Laser Projector (Draft)",    "High-output RGB laser module — listing under review before publication.",                                         null,                           false, 6),
        (7, "Pixel Bar Controller (Beta)","Artnet pixel bar driver currently in beta testing. Not yet available for booking.",                               null,                           false, 7),
    ];

    /// <summary>
    /// Executes the seed async operation.
    /// </summary>
    /// <remarks>Potential side effects: may modify state as part of this operation.</remarks>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SeedAsync(CancellationToken ct)
    {
        var devices = await db.Devices
            .AsNoTracking()
            .OrderBy(d => d.Id)
            .Take(CatalogueDefs.Length)
            .ToListAsync(ct);

        foreach (var (deviceIndex, name, description, photoUrl, isPublished, sortOrder) in CatalogueDefs)
        {
            if (deviceIndex >= devices.Count)
                continue;

            await catalogueService.CreateItem(new CreateCatalogueItemDto
            {
                DeviceGuid  = devices[deviceIndex].Guid,
                Name        = name,
                Description = description,
                PhotoUrl    = photoUrl,
                IsPublished = isPublished,
                SortOrder   = sortOrder,
            }, ct);
        }
    }
}
