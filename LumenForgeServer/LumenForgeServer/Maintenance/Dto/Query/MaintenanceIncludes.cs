namespace LumenForgeServer.Maintenance.Dto.Query;

[Flags]
public enum MaintenanceJobInclude
{
    None = 0,
    Devices = 1,
    Tasks = 2,
    Logs = 4,
    RelatedJobs = 8,
    RelatedRental = 16,
}

[Flags]
public enum MaintenanceTaskInclude
{
    None = 0,
    Devices = 1,
    Logs = 2,
}
