using LumenForgeServer.Inventory.Domain;
using NodaTime;

namespace LumenForgeServer.Common.Database.Seeding.Seeders;

/// <summary>Seeds the default maintenance status. Runs in all environments; idempotent.</summary>
public class MaintenanceStatusSeeder(AppDbContext db) : IDataSeeder
{
    public int Order => 10;
    public SeedEnvironment Environment => SeedEnvironment.All;

    /// <summary>
    /// Executes the seed async operation.
    /// </summary>
    /// <remarks>Potential side effects: may modify state as part of this operation.</remarks>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SeedAsync(CancellationToken ct)
    {
        if (db.MaintenanceStatuses.Any())
            return;

        var now = SystemClock.Instance.GetCurrentInstant();
        db.MaintenanceStatuses.Add(new MaintenanceStatus
        {
            Uuid        = Guid.CreateVersion7(),
            Name        = "Operational",
            Description = "Default maintenance status.",
            CreatedAt   = now,
            UpdatedAt   = now,
        });
        await db.SaveChangesAsync(ct);
    }
}
