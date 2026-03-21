namespace LumenForgeServer.Rentals.Domain;

/// <summary>
/// Stages of the rental process. The current stage of a <see cref="RentalProcessInstance"/>
/// determines which <see cref="Actions.RentalActionType"/> actions are available next.
/// Transitions between stages are driven exclusively by action handlers.
/// </summary>
public enum RentalStage
{
    /// <summary>No process exists yet. Only the <c>CreateRental</c> action is valid.</summary>
    None,

    /// <summary>A rental request has been submitted and awaits staff review.</summary>
    Requested,

    /// <summary>The request has been approved; inventory items can now be assigned.</summary>
    Approved,

    /// <summary>Inventory items have been assigned and are awaiting item-level approval.</summary>
    ItemsAssigned,

    /// <summary>Assigned items have been approved; checklists can be generated.</summary>
    ItemsApproved,

    /// <summary>A pickup checklist has been generated and is ready for scanning/signing.</summary>
    ReadyForPickup,

    /// <summary>The customer has picked up the items; the rental is actively in use.</summary>
    PickedUp,

    /// <summary>The customer has returned the items; post-return inspection is pending.</summary>
    Returned,

    /// <summary>Damages have been recorded and maintenance jobs may be created.</summary>
    Inspected,

    /// <summary>An invoice has been generated for the rental.</summary>
    Invoiced,

    /// <summary>Payment has been received; the rental can be completed.</summary>
    Paid,

    /// <summary>The rental has been successfully completed and archived.</summary>
    Completed,

    /// <summary>The rental was cancelled before completion.</summary>
    Cancelled,

    /// <summary>The rental was scrapped (total write-off).</summary>
    Scrapped
}
