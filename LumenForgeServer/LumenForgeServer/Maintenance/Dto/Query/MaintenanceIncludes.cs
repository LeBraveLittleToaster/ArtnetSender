namespace LumenForgeServer.Maintenance.Dto.Query;

/// <summary>Flags controlling which related entities to include when listing maintenance jobs.</summary>
[Flags]
public enum MaintenanceJobInclude
{
    /// <summary>No related entities.</summary>
    None = 0,
    /// <summary>Include affected devices.</summary>
    Devices = 1,
    /// <summary>Include child tasks.</summary>
    Tasks = 2,
    /// <summary>Include task log entries.</summary>
    Logs = 4,
    /// <summary>Include related job references.</summary>
    RelatedJobs = 8,
    /// <summary>Include linked rental reference.</summary>
    RelatedRental = 16,
}

/// <summary>Flags controlling which related entities to include when listing maintenance tasks.</summary>
[Flags]
public enum MaintenanceTaskInclude
{
    /// <summary>No related entities.</summary>
    None = 0,
    /// <summary>Include affected devices.</summary>
    Devices = 1,
    /// <summary>Include task log entries.</summary>
    Logs = 2,
}
