using LumenForgeServer.Auth.Domain;

namespace LumenForgeServer.Rentals.Service.Actions;

/// <summary>
/// Identifies every discrete action that can be performed on a rental process.
/// Each value maps 1-to-1 to an <see cref="IRentalActionHandler"/> implementation
/// and to a dedicated API endpoint on the <c>RentalActionController</c>.
/// </summary>
public enum RentalActionType
{
    /// <summary>Create a new rental request and its backing <see cref="Domain.RentalProcessInstance"/>.</summary>
    CreateRental,

    /// <summary>Approve an incoming rental request.</summary>
    ApproveRequest,

    /// <summary>Reject an incoming rental request.</summary>
    RejectRequest,

    /// <summary>Assign inventory items (create stock bindings) to the rental.</summary>
    AssignItems,

    /// <summary>Remove previously assigned inventory items from the rental.</summary>
    RemoveItems,

    /// <summary>Approve the assigned item list.</summary>
    ApproveItems,

    /// <summary>Reject the assigned item list and request changes.</summary>
    RejectItems,

    /// <summary>Generate a pickup/dropoff checklist for the rental.</summary>
    GenerateChecklist,

    /// <summary>Record a device scan against the checklist.</summary>
    ScanChecklist,

    /// <summary>Record a signature on the checklist.</summary>
    SignChecklist,

    /// <summary>Record that the customer has picked up the items.</summary>
    RecordPickup,

    /// <summary>Record that the customer has returned the items.</summary>
    RecordReturn,

    /// <summary>Request an extension of the rental period.</summary>
    RequestExtension,

    /// <summary>Approve a requested rental extension.</summary>
    ApproveExtension,

    /// <summary>Reject a requested rental extension.</summary>
    RejectExtension,

    /// <summary>Record damages found during post-return inspection.</summary>
    RecordDamages,

    /// <summary>Create maintenance jobs for damaged items.</summary>
    CreateMaintenanceJobs,

    /// <summary>Generate an invoice for the rental.</summary>
    GenerateInvoice,

    /// <summary>Record a payment against the rental invoice.</summary>
    RecordPayment,

    /// <summary>Generate a summary report for the completed rental.</summary>
    GenerateReport,

    /// <summary>Mark the rental as completed.</summary>
    CompleteRental,

    /// <summary>Cancel the rental.</summary>
    CancelRental,

    /// <summary>Scrap the rental (total write-off of assigned items).</summary>
    ScrapRental
}

public static class RentalActionTypeExtensions
{
    public static List<Permissions> GetNeededPermissions(this RentalActionType actionType)
    {
        return actionType switch
        {
            RentalActionType.CreateRental => [],
            RentalActionType.ApproveRequest => [Permissions.RentalActionCall, Permissions.RentalActionUpdate],
            RentalActionType.RejectRequest => [Permissions.RentalActionCall, Permissions.RentalActionUpdate],
            RentalActionType.AssignItems => [Permissions.RentalActionCall, Permissions.RentalActionUpdate],
            RentalActionType.RemoveItems => [Permissions.RentalActionCall, Permissions.RentalActionUpdate],
            RentalActionType.ApproveItems => [],
            RentalActionType.RejectItems => [],
            RentalActionType.GenerateChecklist => [Permissions.RentalActionCall, Permissions.RentalActionUpdate],
            RentalActionType.ScanChecklist => [Permissions.RentalActionCall, Permissions.RentalActionUpdate],
            RentalActionType.SignChecklist => [Permissions.RentalActionCall, Permissions.RentalActionUpdate],
            RentalActionType.RecordPickup => [Permissions.RentalActionCall, Permissions.RentalActionUpdate],
            RentalActionType.RecordReturn => [Permissions.RentalActionCall, Permissions.RentalActionUpdate],
            RentalActionType.RequestExtension => [],
            RentalActionType.ApproveExtension => [Permissions.RentalActionCall, Permissions.RentalActionUpdate],
            RentalActionType.RejectExtension => [Permissions.RentalActionCall, Permissions.RentalActionUpdate],
            RentalActionType.RecordDamages => [],
            RentalActionType.CreateMaintenanceJobs => [Permissions.RentalActionCall, Permissions.RentalActionUpdate],
            RentalActionType.GenerateInvoice => [Permissions.RentalActionCall, Permissions.RentalActionUpdate],
            RentalActionType.RecordPayment => [Permissions.RentalActionCall, Permissions.RentalActionUpdate],
            RentalActionType.GenerateReport => [Permissions.RentalActionCall, Permissions.RentalActionUpdate],
            RentalActionType.CompleteRental => [Permissions.RentalActionCall, Permissions.RentalActionUpdate],
            RentalActionType.CancelRental => [],
            RentalActionType.ScrapRental => [Permissions.RentalActionCall, Permissions.RentalActionUpdate],
            _ => throw new ArgumentOutOfRangeException(nameof(actionType), actionType, null)
        };
    }
}
