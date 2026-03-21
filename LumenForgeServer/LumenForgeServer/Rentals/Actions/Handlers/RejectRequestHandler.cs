using LumenForgeServer.Rentals.Domain;

namespace LumenForgeServer.Rentals.Actions.Handlers;

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
public sealed class RejectRequestHandler : RentalActionHandlerBase<RejectRequestInput>
{
    /// <inheritdoc />
    public override RentalActionType ActionType => RentalActionType.RejectRequest;

    /// <inheritdoc />
    public override IReadOnlySet<RentalStage> AllowedStages { get; } =
        new HashSet<RentalStage> { RentalStage.Requested };

    /// <inheritdoc />
    protected override Task<ActionResult> ExecuteAsync(
        RentalProcessInstance process, RejectRequestInput input, CancellationToken ct)
    {
        return Task.FromResult(ActionResult.Ok(nameof(RentalActionType.RejectRequest), RentalStage.Cancelled));
    }
}
