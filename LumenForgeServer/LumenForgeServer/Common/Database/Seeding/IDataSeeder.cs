namespace LumenForgeServer.Common.Database.Seeding;

public interface IDataSeeder
{
    /// <summary>Ascending execution order within a run.</summary>
    int Order { get; }

    /// <summary>Which environments this seeder should run in.</summary>
    SeedEnvironment Environment { get; }

    Task SeedAsync(CancellationToken ct);
}
