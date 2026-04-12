using LumenForgeServer.Rentals.Domain;
using NodaTime;

namespace LumenForgeServer.Rentals.Service.Actions.Handlers;

/// <summary>Input for the <see cref="CompleteRentalHandler"/>.</summary>
public sealed class CompleteRentalInput : ActionInput
{
    /// <summary>Optional closing comment.</summary>
    public string? Comment { get; init; }
}

/// <summary>
/// Marks the rental as completed, archiving the process.
/// Transitions to <see cref="RentalStage.Completed"/> — a terminal state.
/// </summary>
public sealed class CompleteRentalHandler : RentalActionHandlerBase<CompleteRentalInput, BlankActionResult>
{
    /// <inheritdoc />
    public override RentalActionType ActionType => RentalActionType.CompleteRental;

    /// <inheritdoc />
    public override IReadOnlySet<RentalStage> AllowedStages { get; } =
        new HashSet<RentalStage> { RentalStage.Paid };

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
    protected override async Task<BlankActionResult> BeforeExecuteAsync(RentalProcessInstance process, CompleteRentalInput input, CancellationToken ct)
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
        RentalProcessInstance process, CompleteRentalInput input, CancellationToken ct)
    {
        if (input.Comment is not null && process.Rental is not null)
        {
            process.Rental.Notes = string.IsNullOrEmpty(process.Rental.Notes)
                ? $"[Completed] {input.Comment}"
                : $"{process.Rental.Notes}\n[Completed] {input.Comment}";
            process.Rental.UpdatedAt = SystemClock.Instance.GetCurrentInstant();
        }

        return Task.FromResult(BlankActionResult.Ok(nameof(RentalActionType.CompleteRental), RentalStage.Completed));
    }
}
