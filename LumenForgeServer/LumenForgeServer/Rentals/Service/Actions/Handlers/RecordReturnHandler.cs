using LumenForgeServer.Rentals.Domain;

namespace LumenForgeServer.Rentals.Service.Actions.Handlers;

/// <summary>Input for the <see cref="RecordReturnHandler"/>.</summary>
public sealed class RecordReturnInput : ActionInput
{
    /// <summary>Optional notes recorded at return time.</summary>
    public string? Notes { get; init; }
}

/// <summary>
/// Records that the customer has returned the rental items.
/// Transitions the process to <see cref="RentalStage.Returned"/>.
/// </summary>
public sealed class RecordReturnHandler : RentalActionHandlerBase<RecordReturnInput, BlankActionResult>
{
    /// <inheritdoc />
    public override RentalActionType ActionType => RentalActionType.RecordReturn;

    /// <inheritdoc />
    public override IReadOnlySet<RentalStage> AllowedStages { get; } =
        new HashSet<RentalStage> { RentalStage.PickedUp };

    /// <summary>
    /// Executes the after execute async operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: may persist state changes, emit workflow logs, or call external dependencies.</remarks>
    /// <param name="process">Input value used by this operation.</param>
    /// <param name="result">Input value used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected override async Task AfterExecuteAsync(RentalProcessInstance process, BlankActionResult result, CancellationToken ct)
    {
        
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
    protected override async Task<BlankActionResult> BeforeExecuteAsync(RentalProcessInstance process, RecordReturnInput input, CancellationToken ct)
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
        RentalProcessInstance process, RecordReturnInput input, CancellationToken ct)
    {
        if (input.Notes is not null && process.Rental is not null)
        {
            process.Rental.Notes = string.IsNullOrEmpty(process.Rental.Notes)
                ? $"[Return] {input.Notes}"
                : $"{process.Rental.Notes}\n[Return] {input.Notes}";
        }

        return Task.FromResult(BlankActionResult.Ok(nameof(RentalActionType.RecordReturn), RentalStage.Returned));
    }
}
