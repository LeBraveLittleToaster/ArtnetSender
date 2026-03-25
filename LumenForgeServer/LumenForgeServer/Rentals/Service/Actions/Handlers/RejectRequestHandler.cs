using LumenForgeServer.Rentals.Domain;

namespace LumenForgeServer.Rentals.Service.Actions.Handlers;

/// <summary>Input for the <see cref="RejectRequestHandler"/>.</summary>
public sealed class RejectRequestInput : ActionInput
{
    /// <summary>Reason for rejecting the request.</summary>
    public required string Reason { get; init; }
}

/// <summary>
/// Rejects an incoming rental request, transitioning the process to
/// <see cref="RentalStage.Cancelled"/>. A rejection reason is required.
/// </summary>
public sealed class RejectRequestHandler : RentalActionHandlerBase<RejectRequestInput, BlankActionResult>
{
    /// <inheritdoc />
    public override RentalActionType ActionType => RentalActionType.RejectRequest;

    /// <inheritdoc />
    public override IReadOnlySet<RentalStage> AllowedStages { get; } =
        new HashSet<RentalStage> { RentalStage.Requested };

    protected override async Task AfterExecuteAsync(RentalProcessInstance process, BlankActionResult result, CancellationToken ct)
    {

    }

    protected override async Task<BlankActionResult> BeforeExecuteAsync(RentalProcessInstance process, RejectRequestInput input, CancellationToken ct)
    {
        return BlankActionResult.Ok(this.ActionType.ToString());
    }

    /// <inheritdoc />
    protected override Task<BlankActionResult> ExecuteAsync(
        RentalProcessInstance process, RejectRequestInput input, CancellationToken ct)
    {
        return Task.FromResult(BlankActionResult.Ok(nameof(RentalActionType.RejectRequest), RentalStage.Cancelled));
    }
}
