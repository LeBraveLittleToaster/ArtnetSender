using LumenForgeServer.Inventory.Dto.Create;
using LumenForgeServer.Inventory.Service;

namespace LumenForgeServer.Common.Database.Seeding.Seeders;

/// <summary>Seeds real-world vendors and event-tech categories loaded from embedded CSVs. Dev only.</summary>
public class VendorCategorySeeder(VendorService vendorService, CategoryService categoryService) : IDataSeeder
{
    public int Order => 20;
    public SeedEnvironment Environment => SeedEnvironment.Dev;

    /// <summary>
    /// Executes the seed async operation.
    /// </summary>
    /// <remarks>Potential side effects: may modify state as part of this operation.</remarks>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SeedAsync(CancellationToken ct)
    {
        foreach (var row in SeedDataLoader.Load("vendors.csv"))
        {
            if (row.Length < 1) continue;
            await vendorService.CreateVendor(new CreateVendorDto { Name = row[0].Trim() }, ct);
        }

        foreach (var row in SeedDataLoader.Load("categories.csv"))
        {
            if (row.Length < 2) continue;
            await categoryService.CreateCategory(
                new CreateCategoryDto { Name = row[0].Trim(), Description = row[1].Trim() },
                ct);
        }
    }
}
