using System.Text.Json.Serialization;

namespace LumenForgeServer.Rentals.Domain;

/// <summary>
/// Identifies a preprogrammed rental action that a user can invoke.
/// Each value corresponds to a concrete <see cref="Actions.RentalAction"/> subclass
/// and its associated handler.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RentalActionType
{
    ApproveRequest,
    RejectRequest,
    AssignItems,
    RemoveItems,
    ApproveItems,
    RejectItems,
    GenerateChecklist,
    ScanChecklist,
    SignChecklist,
    RecordPickup,
    RequestExtension,
    ApproveExtension,
    RejectExtension,
    RecordReturn,
    RecordDamages,
    CreateMaintenanceJobs,
    GenerateInvoice,
    RecordPayment,
    GenerateReport,
    CompleteRental,
    CancelRental,
    ScrapRental,
}
