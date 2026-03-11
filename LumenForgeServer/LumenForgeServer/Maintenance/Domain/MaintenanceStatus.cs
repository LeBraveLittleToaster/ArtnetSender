namespace LumenForgeServer.Maintenance.Domain
{
    /// <summary>
    /// The MaintenanceStatus enum represents the various states that a maintenance task can be in during its lifecycle. It provides a standardized way to track the progress of maintenance tasks and to identify any issues or bottlenecks in the maintenance process. The different statuses allow for clear communication and coordination among team members, as well as providing valuable data for analysis and reporting on maintenance activities.
    /// </summary>
    public enum MaintenanceStatus
    {
        /// <summary>
        /// Maintenance Need reported but not yet investigated or assigned for resolution. 
        /// </summary>
        Reported = 0,
        /// <summary>
        /// Maintenance issue is currently being investigated or worked on.
        /// </summary>
        UnderInvestigation = 1,
        /// <summary>
        /// Indicates that the item has been resolved.
        /// </summary>
        Resolved = 2,
        /// <summary>
        /// Maintenance issue cannot be resolved due to external factors (e.g., lack of parts, end-of-life device) or is deemed not worth resolving.
        /// </summary>
        NotResolvable = 3,
        /// <summary>
        /// Indicates that additional information is required to complete the operation or process.
        /// </summary>
        InformationNeeded = 4,
        /// <summary>
        /// Indicates that the maintenance issue is currently on hold while waiting for necessary parts or resources to become available.
        /// </summary>
        WaitingForResources = 5,
        /// <summary>
        /// Indicates that no maintenance is required.
        /// </summary>
        NoMaintenanceNeeded = 6,
    }
}
