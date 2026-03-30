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

    /// <summary>
    /// Executes the after execute async operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: may persist state changes, emit workflow logs, or call external dependencies.</remarks>
    /// <param name="process">Input value used by this operation.</param>
    /// <param name="result">Input value used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected override Task AfterExecuteAsync(RentalProcessInstance process, BlankActionResult result, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Executes the before execute async operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="process">Input value used by this operation.</param>
    /// <param name="input">Request payload containing the input data required for the operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the BlankActionResult result.</returns>
    protected override async Task<BlankActionResult> BeforeExecuteAsync(RentalProcessInstance process, ApproveRequestInput input, CancellationToken ct)
    {
        return BlankActionResult.Ok(this.ActionType.ToString());
    }

    /// <inheritdoc />
    /// <summary>
    /// Executes the execute async operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: may persist state changes, emit workflow logs, or call external dependencies.</remarks>
    /// <param name="process">Input value used by this operation.</param>
    /// <param name="input">Request payload containing the input data required for the operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the BlankActionResult result.</returns>
    protected override Task<BlankActionResult> ExecuteAsync(
        RentalProcessInstance process, ApproveRequestInput input, CancellationToken ct)
    {
        return Task.FromResult(BlankActionResult.Ok(nameof(RentalActionType.ApproveRequest), RentalStage.Approved));
    }
}
