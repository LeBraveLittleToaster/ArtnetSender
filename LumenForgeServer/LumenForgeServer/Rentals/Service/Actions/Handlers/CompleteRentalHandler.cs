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

    protected override Task AfterExecuteAsync(RentalProcessInstance process, BlankActionResult result, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    protected override async Task<BlankActionResult> BeforeExecuteAsync(RentalProcessInstance process, CompleteRentalInput input, CancellationToken ct)
    {
        return BlankActionResult.Ok(this.ActionType.ToString());
    }

    /// <inheritdoc />
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
