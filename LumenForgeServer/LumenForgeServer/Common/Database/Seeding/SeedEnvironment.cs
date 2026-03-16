namespace LumenForgeServer.Common.Database.Seeding;

[Flags]
public enum SeedEnvironment
{
    Dev        = 1,
    Production = 2,
    All        = Dev | Production,
}
