using LumenForgeServer.Rentals.Domain;

namespace LumenForgeServer.Rentals.Service.Actions.Handlers;

/// <summary>Input for the <see cref="ApproveItemsHandler"/>.</summary>
public sealed class ApproveItemsInput : ActionInput
{
    /// <summary>Optional comment from the approver.</summary>
    public string? Comment { get; init; }
}

/// <summary>
/// Approves the currently assigned item list, transitioning the process
/// from <see cref="RentalStage.ItemsAssigned"/> to
/// <see cref="RentalStage.ItemsApproved"/>.
/// </summary>
public sealed class ApproveItemsHandler : RentalActionHandlerBase<ApproveItemsInput>
{
    /// <inheritdoc />
    public override RentalActionType ActionType => RentalActionType.ApproveItems;

    /// <inheritdoc />
    public override IReadOnlySet<RentalStage> AllowedStages { get; } =
        new HashSet<RentalStage> { RentalStage.ItemsAssigned };

    /// <inheritdoc />
    protected override Task<ActionResult> ExecuteAsync(
        RentalProcessInstance process, ApproveItemsInput input, CancellationToken ct)
    {
        return Task.FromResult(ActionResult.Ok(nameof(RentalActionType.ApproveItems), RentalStage.ItemsApproved));
    }
}
