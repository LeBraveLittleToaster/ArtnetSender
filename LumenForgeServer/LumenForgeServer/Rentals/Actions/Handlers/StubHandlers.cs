using System.Text.Json;
using LumenForgeServer.Common;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Domain.Actions;

namespace LumenForgeServer.Rentals.Actions.Handlers;

// ---------------------------------------------------------------------------
// Stub handlers — CanExecute is fully implemented so the "available actions"
// endpoint works correctly. ExecuteAsync throws until business logic is ported.
// ---------------------------------------------------------------------------

/// <summary>Assigns rental items to the rental.</summary>
public sealed class AssignItemsHandler : IRentalActionHandler
{
    public RentalActionType ActionType => RentalActionType.AssignItems;

    public bool CanExecute(Rental rental)
        => rental.RentalStatus is RentalStatus.Requested or RentalStatus.Approved;

    public Task<RentalAction> ExecuteAsync(Rental rental, JsonElement? input, string actorUserId, CancellationToken ct)
        => throw new NotImplementedException("AssignItems handler not yet implemented.");
}

/// <summary>Removes rental items from the rental.</summary>
public sealed class RemoveItemsHandler : IRentalActionHandler
{
    public RentalActionType ActionType => RentalActionType.RemoveItems;

    public bool CanExecute(Rental rental)
        => rental.RentalStatus is RentalStatus.Requested or RentalStatus.Approved
           && rental.Items.Count > 0;

    public Task<RentalAction> ExecuteAsync(Rental rental, JsonElement? input, string actorUserId, CancellationToken ct)
        => throw new NotImplementedException("RemoveItems handler not yet implemented.");
}

/// <summary>Approves individual line items (qty, pricing).</summary>
public sealed class ApproveItemsHandler : IRentalActionHandler
{
    public RentalActionType ActionType => RentalActionType.ApproveItems;

    public bool CanExecute(Rental rental)
        => rental.RentalStatus is RentalStatus.Requested or RentalStatus.Approved
           && rental.Items.Any(i => i.Status == RentalItemStatus.REQUESTED);

    public Task<RentalAction> ExecuteAsync(Rental rental, JsonElement? input, string actorUserId, CancellationToken ct)
        => throw new NotImplementedException("ApproveItems handler not yet implemented.");
}

/// <summary>Rejects individual line items.</summary>
public sealed class RejectItemsHandler : IRentalActionHandler
{
    public RentalActionType ActionType => RentalActionType.RejectItems;

    public bool CanExecute(Rental rental)
        => rental.RentalStatus is RentalStatus.Requested or RentalStatus.Approved
           && rental.Items.Any(i => i.Status == RentalItemStatus.REQUESTED);

    public Task<RentalAction> ExecuteAsync(Rental rental, JsonElement? input, string actorUserId, CancellationToken ct)
        => throw new NotImplementedException("RejectItems handler not yet implemented.");
}

/// <summary>Generates a pickup or dropoff checklist from approved items.</summary>
public sealed class GenerateChecklistHandler : IRentalActionHandler
{
    public RentalActionType ActionType => RentalActionType.GenerateChecklist;

    public bool CanExecute(Rental rental)
        => rental.RentalStatus is RentalStatus.Approved or RentalStatus.PickedUp
           && rental.Items.Any(i => i.Status is RentalItemStatus.APPROVED or RentalItemStatus.PARTIALLY_APPROVED);

    public Task<RentalAction> ExecuteAsync(Rental rental, JsonElement? input, string actorUserId, CancellationToken ct)
        => throw new NotImplementedException("GenerateChecklist handler not yet implemented.");
}

/// <summary>Scans/inspects checklist items.</summary>
public sealed class ScanChecklistHandler : IRentalActionHandler
{
    public RentalActionType ActionType => RentalActionType.ScanChecklist;

    public bool CanExecute(Rental rental)
        => rental.RentalStatus is RentalStatus.Approved or RentalStatus.PickedUp
           && rental.Checklists.Any(c => c.SignedAt is null);

    public Task<RentalAction> ExecuteAsync(Rental rental, JsonElement? input, string actorUserId, CancellationToken ct)
        => throw new NotImplementedException("ScanChecklist handler not yet implemented.");
}

/// <summary>Signs a checklist, making it immutable.</summary>
public sealed class SignChecklistHandler : IRentalActionHandler
{
    public RentalActionType ActionType => RentalActionType.SignChecklist;

    public bool CanExecute(Rental rental)
        => rental.RentalStatus is RentalStatus.Approved or RentalStatus.PickedUp
           && rental.Checklists.Any(c => c.SignedAt is null);

    public Task<RentalAction> ExecuteAsync(Rental rental, JsonElement? input, string actorUserId, CancellationToken ct)
        => throw new NotImplementedException("SignChecklist handler not yet implemented.");
}

/// <summary>Customer requests a return-date extension.</summary>
public sealed class RequestExtensionHandler : IRentalActionHandler
{
    public RentalActionType ActionType => RentalActionType.RequestExtension;

    public bool CanExecute(Rental rental)
        => rental.RentalStatus == RentalStatus.PickedUp;

    public Task<RentalAction> ExecuteAsync(Rental rental, JsonElement? input, string actorUserId, CancellationToken ct)
        => throw new NotImplementedException("RequestExtension handler not yet implemented.");
}

/// <summary>Staff approves a pending extension.</summary>
public sealed class ApproveExtensionHandler : IRentalActionHandler
{
    public RentalActionType ActionType => RentalActionType.ApproveExtension;

    public bool CanExecute(Rental rental)
        => rental.RentalStatus == RentalStatus.PickedUp
           && rental.Extensions.Any(e => !e.IsApproved && e.RejectionReason is null);

    public Task<RentalAction> ExecuteAsync(Rental rental, JsonElement? input, string actorUserId, CancellationToken ct)
        => throw new NotImplementedException("ApproveExtension handler not yet implemented.");
}

/// <summary>Staff rejects a pending extension.</summary>
public sealed class RejectExtensionHandler : IRentalActionHandler
{
    public RentalActionType ActionType => RentalActionType.RejectExtension;

    public bool CanExecute(Rental rental)
        => rental.RentalStatus == RentalStatus.PickedUp
           && rental.Extensions.Any(e => !e.IsApproved && e.RejectionReason is null);

    public Task<RentalAction> ExecuteAsync(Rental rental, JsonElement? input, string actorUserId, CancellationToken ct)
        => throw new NotImplementedException("RejectExtension handler not yet implemented.");
}

/// <summary>Records damage reports on returned items.</summary>
public sealed class RecordDamagesHandler : IRentalActionHandler
{
    public RentalActionType ActionType => RentalActionType.RecordDamages;

    public bool CanExecute(Rental rental)
        => rental.RentalStatus == RentalStatus.Returned;

    public Task<RentalAction> ExecuteAsync(Rental rental, JsonElement? input, string actorUserId, CancellationToken ct)
        => throw new NotImplementedException("RecordDamages handler not yet implemented.");
}

/// <summary>Spawns maintenance jobs from damage reports.</summary>
public sealed class CreateMaintenanceJobsHandler : IRentalActionHandler
{
    public RentalActionType ActionType => RentalActionType.CreateMaintenanceJobs;

    public bool CanExecute(Rental rental)
        => rental.RentalStatus == RentalStatus.Returned;

    public Task<RentalAction> ExecuteAsync(Rental rental, JsonElement? input, string actorUserId, CancellationToken ct)
        => throw new NotImplementedException("CreateMaintenanceJobs handler not yet implemented.");
}

/// <summary>Generates an invoice for the rental.</summary>
public sealed class GenerateInvoiceHandler : IRentalActionHandler
{
    public RentalActionType ActionType => RentalActionType.GenerateInvoice;

    public bool CanExecute(Rental rental)
        => rental.RentalStatus == RentalStatus.Returned;

    public Task<RentalAction> ExecuteAsync(Rental rental, JsonElement? input, string actorUserId, CancellationToken ct)
        => throw new NotImplementedException("GenerateInvoice handler not yet implemented.");
}

/// <summary>Records a payment against an invoice.</summary>
public sealed class RecordPaymentHandler : IRentalActionHandler
{
    public RentalActionType ActionType => RentalActionType.RecordPayment;

    public bool CanExecute(Rental rental)
        => rental.RentalStatus is RentalStatus.Returned or RentalStatus.Completed;

    public Task<RentalAction> ExecuteAsync(Rental rental, JsonElement? input, string actorUserId, CancellationToken ct)
        => throw new NotImplementedException("RecordPayment handler not yet implemented.");
}

/// <summary>Generates the final rental report.</summary>
public sealed class GenerateReportHandler : IRentalActionHandler
{
    public RentalActionType ActionType => RentalActionType.GenerateReport;

    public bool CanExecute(Rental rental)
        => rental.RentalStatus is RentalStatus.Returned or RentalStatus.Completed
           && rental.RentalReport is null;

    public Task<RentalAction> ExecuteAsync(Rental rental, JsonElement? input, string actorUserId, CancellationToken ct)
        => throw new NotImplementedException("GenerateReport handler not yet implemented.");
}
