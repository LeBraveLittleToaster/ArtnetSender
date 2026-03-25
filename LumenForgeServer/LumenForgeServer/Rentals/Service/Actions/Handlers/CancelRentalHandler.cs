using LumenForgeServer.Rentals.Domain;
using NodaTime;

namespace LumenForgeServer.Rentals.Service.Actions.Handlers;

/// <summary>Input for the <see cref="CancelRentalHandler"/>.</summary>
public sealed class CancelRentalInput : ActionInput
{
    /// <summary>Reason for cancellation.</summary>
    public required string Reason { get; init; }
}

/// <summary>
/// Cancels the rental before items have been picked up.
/// Transitions to <see cref="RentalStage.Cancelled"/> — a terminal state.
/// Any existing stock bindings are released.
/// </summary>
public sealed class CancelRentalHandler : RentalActionHandlerBase<CancelRentalInput, BlankActionResult>
{
    /// <inheritdoc />
    public override RentalActionType ActionType => RentalActionType.CancelRental;

    /// <inheritdoc />
    public override IReadOnlySet<RentalStage> AllowedStages { get; } =
        new HashSet<RentalStage>
        {
            RentalStage.Requested,
            RentalStage.Approved,
            RentalStage.ItemsAssigned,
            RentalStage.ItemsApproved,
            RentalStage.ReadyForPickup
        };

    protected override Task AfterExecuteAsync(RentalProcessInstance process, BlankActionResult result, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    protected override async Task<BlankActionResult> BeforeExecuteAsync(RentalProcessInstance process, CancelRentalInput input, CancellationToken ct)
    {
        return BlankActionResult.Ok(this.ActionType.ToString());
    }

    /// <inheritdoc />
    protected override Task<BlankActionResult> ExecuteAsync(
        RentalProcessInstance process, CancelRentalInput input, CancellationToken ct)
    {
        if (process.Rental is not null)
        {
            process.Rental.Notes = string.IsNullOrEmpty(process.Rental.Notes)
                ? $"[Cancelled] {input.Reason}"
                : $"{process.Rental.Notes}\n[Cancelled] {input.Reason}";
            process.Rental.UpdatedAt = SystemClock.Instance.GetCurrentInstant();
        }

        return Task.FromResult(BlankActionResult.Ok(nameof(RentalActionType.CancelRental), RentalStage.Cancelled));
    }
}
