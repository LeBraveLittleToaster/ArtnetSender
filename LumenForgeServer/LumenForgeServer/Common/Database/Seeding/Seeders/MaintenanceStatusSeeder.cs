using LumenForgeServer.Inventory.Domain;
using NodaTime;

namespace LumenForgeServer.Common.Database.Seeding.Seeders;

/// <summary>Seeds the default maintenance status. Runs in all environments; idempotent.</summary>
public class MaintenanceStatusSeeder(AppDbContext db) : IDataSeeder
{
    public int Order => 10;
    public SeedEnvironment Environment => SeedEnvironment.All;

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
