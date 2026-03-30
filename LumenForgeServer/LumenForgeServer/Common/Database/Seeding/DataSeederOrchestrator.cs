namespace LumenForgeServer.Common.Database.Seeding;

/// <summary>
/// Resolves all registered <see cref="IDataSeeder"/> implementations, filters them by
/// environment and runs them in <see cref="IDataSeeder.Order"/> order.
/// </summary>
public class DataSeederOrchestrator(
    IEnumerable<IDataSeeder> seeders,
    AppDbContext db,
    ILogger<DataSeederOrchestrator> logger)
{
    /// <summary>
    /// Runs all seeders applicable to <paramref name="env"/>.
    /// In <see cref="SeedEnvironment.Dev"/> the database is wiped and recreated first.
    /// </summary>
    /// <param name="env">Input value used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    public async Task RunAsync(SeedEnvironment env, CancellationToken ct)
    {
        if (env == SeedEnvironment.Dev)
        {
            logger.LogWarning("Deleting database...");
            await db.Database.EnsureDeletedAsync(ct);
            logger.LogInformation("Recreating database schema...");
            await db.Database.EnsureCreatedAsync(ct);
        }

        var applicable = seeders
            .Where(s => s.Environment.HasFlag(env))
            .OrderBy(s => s.Order)
            .ToList();

        logger.LogInformation("Running {Count} seeders for environment {Env}.", applicable.Count, env);

        foreach (var seeder in applicable)
        {
            logger.LogInformation("[{Seeder}] starting...", seeder.GetType().Name);
            await seeder.SeedAsync(ct);
            logger.LogInformation("[{Seeder}] done.", seeder.GetType().Name);
        }

        logger.LogInformation("All seeders completed.");
    }
}
