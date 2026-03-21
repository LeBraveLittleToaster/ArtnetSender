using LumenForgeServer.Rentals.Domain;

namespace LumenForgeServer.Rentals.Actions.Handlers;

/// <summary>Input for the <see cref="ApproveRequestHandler"/>.</summary>
public sealed class ApproveRequestInput : ActionInput
{
    /// <summary>Optional comment from the approver.</summary>
    public string? Comment { get; init; }
}

/// <summary>
/// Approves an incoming rental request, transitioning the process from
/// <see cref="RentalStage.Requested"/> to <see cref="RentalStage.Approved"/>.
/// After approval, inventory items can be assigned.
/// </summary>
public sealed class ApproveRequestHandler : RentalActionHandlerBase<ApproveRequestInput>
{
    /// <inheritdoc />
    public override RentalActionType ActionType => RentalActionType.ApproveRequest;

    /// <inheritdoc />
    public override IReadOnlySet<RentalStage> AllowedStages { get; } =
        new HashSet<RentalStage> { RentalStage.Requested };

    /// <inheritdoc />
    protected override Task<ActionResult> ExecuteAsync(
        RentalProcessInstance process, ApproveRequestInput input, CancellationToken ct)
    {
        return Task.FromResult(ActionResult.Ok(nameof(RentalActionType.ApproveRequest), RentalStage.Approved));
    }
}
