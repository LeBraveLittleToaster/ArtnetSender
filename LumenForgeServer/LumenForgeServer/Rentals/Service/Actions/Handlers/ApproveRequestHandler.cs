using LumenForgeServer.Rentals.Domain;

namespace LumenForgeServer.Rentals.Service.Actions.Handlers;

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
public sealed class ApproveRequestHandler : RentalActionHandlerBase<ApproveRequestInput, BlankActionResult>
{
    /// <inheritdoc />
    public override RentalActionType ActionType => RentalActionType.ApproveRequest;

    /// <inheritdoc />
    public override IReadOnlySet<RentalStage> AllowedStages { get; } =
        new HashSet<RentalStage> { RentalStage.Requested };

    protected override Task AfterExecuteAsync(RentalProcessInstance process, BlankActionResult result, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    protected override async Task<BlankActionResult> BeforeExecuteAsync(RentalProcessInstance process, ApproveRequestInput input, CancellationToken ct)
    {
        return BlankActionResult.Ok(this.ActionType.ToString());
    }

    /// <inheritdoc />
    protected override Task<BlankActionResult> ExecuteAsync(
        RentalProcessInstance process, ApproveRequestInput input, CancellationToken ct)
    {
        return Task.FromResult(BlankActionResult.Ok(nameof(RentalActionType.ApproveRequest), RentalStage.Approved));
    }
}
