using LumenForgeServer.Rentals.Domain;

namespace LumenForgeServer.Rentals.Service.Actions;

/// <summary>
/// Default implementation of <see cref="IRentalActionRegistry"/>.
/// Encodes the allowed actions per rental stage as a static lookup table.
/// Modify this class to add, remove, or reorder transitions.
/// </summary>
public sealed class RentalActionRegistry : IRentalActionRegistry
{
    /// <summary>
    /// Executes the new operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <returns>The operation result.</returns>
    private static readonly Dictionary<RentalStage, IReadOnlySet<RentalActionType>> StageActions = new()
    {
        [RentalStage.None] = new HashSet<RentalActionType>
        {
            RentalActionType.CreateRental
        },

        [RentalStage.Requested] = new HashSet<RentalActionType>
        {
            RentalActionType.ApproveRequest,
            RentalActionType.RejectRequest,
            RentalActionType.CancelRental
        },

        [RentalStage.Approved] = new HashSet<RentalActionType>
        {
            RentalActionType.AssignItems,
            RentalActionType.CancelRental
        },

        [RentalStage.ItemsAssigned] = new HashSet<RentalActionType>
        {
            RentalActionType.AssignItems,
            RentalActionType.RemoveItems,
            RentalActionType.ApproveItems,
            RentalActionType.RejectItems,
            RentalActionType.CancelRental
        },

        [RentalStage.ItemsApproved] = new HashSet<RentalActionType>
        {
            RentalActionType.GenerateChecklist,
            RentalActionType.CancelRental
        },

        [RentalStage.ReadyForPickup] = new HashSet<RentalActionType>
        {
            RentalActionType.ScanChecklist,
            RentalActionType.SignChecklist,
            RentalActionType.RecordPickup,
            RentalActionType.CancelRental
        },

        [RentalStage.PickedUp] = new HashSet<RentalActionType>
        {
            RentalActionType.RecordReturn,
            RentalActionType.RequestExtension,
            RentalActionType.ApproveExtension,
            RentalActionType.RejectExtension,
            RentalActionType.ScrapRental
        },

        [RentalStage.Returned] = new HashSet<RentalActionType>
        {
            RentalActionType.RecordDamages,
            RentalActionType.CreateMaintenanceJobs,
            RentalActionType.GenerateInvoice
        },

        [RentalStage.Inspected] = new HashSet<RentalActionType>
        {
            RentalActionType.CreateMaintenanceJobs,
            RentalActionType.GenerateInvoice
        },

        [RentalStage.Invoiced] = new HashSet<RentalActionType>
        {
            RentalActionType.RecordPayment
        },

        [RentalStage.Paid] = new HashSet<RentalActionType>
        {
            RentalActionType.GenerateReport,
            RentalActionType.CompleteRental
        },

        [RentalStage.Completed] = new HashSet<RentalActionType>
        {
            RentalActionType.GenerateReport
        },

        [RentalStage.Cancelled] = new HashSet<RentalActionType>(),

        [RentalStage.Scrapped] = new HashSet<RentalActionType>
        {
            RentalActionType.GenerateReport
        }
    };
    
    /// <inheritdoc />
    /// <summary>
    /// Executes the get available actions operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="stage">Input value used by this operation.</param>
    /// <returns>The operation result.</returns>
    public IReadOnlySet<RentalActionType> GetAvailableActions(RentalStage stage)
    {
        return !StageActions.TryGetValue(stage, out var actions) 
            ? new HashSet<RentalActionType>() 
            : actions;
    }
}
