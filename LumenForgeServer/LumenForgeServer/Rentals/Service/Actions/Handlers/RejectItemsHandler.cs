using LumenForgeServer.Rentals.Domain;

namespace LumenForgeServer.Rentals.Service.Actions.Handlers;

/// <summary>Input for the <see cref="RejectItemsHandler"/>.</summary>
public sealed class RejectItemsInput : ActionInput
{
    /// <summary>Reason for rejecting the item assignment.</summary>
    public required string Reason { get; init; }
}

/// <summary>
/// Rejects the assigned item list, returning the process to
/// <see cref="RentalStage.Approved"/> so staff can reassign items.
/// </summary>
public sealed class RejectItemsHandler : RentalActionHandlerBase<RejectItemsInput>
{
    /// <inheritdoc />
    public override RentalActionType ActionType => RentalActionType.RejectItems;

    /// <inheritdoc />
    public override IReadOnlySet<RentalStage> AllowedStages { get; } =
        new HashSet<RentalStage> { RentalStage.ItemsAssigned };

    /// <inheritdoc />
    protected override Task<ActionResult> ExecuteAsync(
        RentalProcessInstance process, RejectItemsInput input, CancellationToken ct)
    {
        return Task.FromResult(ActionResult.Ok(nameof(RentalActionType.RejectItems), RentalStage.Approved));
    }
}
