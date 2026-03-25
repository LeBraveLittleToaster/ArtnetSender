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
public sealed class RejectItemsHandler : RentalActionHandlerBase<RejectItemsInput, BlankActionResult>
{
    /// <inheritdoc />
    public override RentalActionType ActionType => RentalActionType.RejectItems;

    /// <inheritdoc />
    public override IReadOnlySet<RentalStage> AllowedStages { get; } =
        new HashSet<RentalStage> { RentalStage.ItemsAssigned };

    protected override async Task AfterExecuteAsync(RentalProcessInstance process, BlankActionResult result, CancellationToken ct)
    {
        
    }

    protected override async Task<BlankActionResult> BeforeExecuteAsync(RentalProcessInstance process, RejectItemsInput input, CancellationToken ct)
    {
        return BlankActionResult.Ok(this.ActionType.ToString());
    }

    /// <inheritdoc />
    protected override Task<BlankActionResult> ExecuteAsync(
        RentalProcessInstance process, RejectItemsInput input, CancellationToken ct)
    {
        return Task.FromResult(BlankActionResult.Ok(nameof(RentalActionType.RejectItems), RentalStage.Approved));
    }
}
