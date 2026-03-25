using LumenForgeServer.Rentals.Domain;

namespace LumenForgeServer.Rentals.Service.Actions.Handlers;

/// <summary>Input for the <see cref="RecordPickupHandler"/>.</summary>
public sealed class RecordPickupInput : ActionInput
{
    /// <summary>Optional notes recorded at pickup time.</summary>
    public string? Notes { get; init; }
}

/// <summary>
/// Records that the customer has physically picked up the rental items.
/// Transitions the process to <see cref="RentalStage.PickedUp"/>.
/// </summary>
public sealed class RecordPickupHandler : RentalActionHandlerBase<RecordPickupInput, BlankActionResult>
{
    /// <inheritdoc />
    public override RentalActionType ActionType => RentalActionType.RecordPickup;

    /// <inheritdoc />
    public override IReadOnlySet<RentalStage> AllowedStages { get; } =
        new HashSet<RentalStage> { RentalStage.ReadyForPickup };

    protected override async Task AfterExecuteAsync(RentalProcessInstance process, BlankActionResult result, CancellationToken ct)
    {
        
    }

    protected override async Task<BlankActionResult> BeforeExecuteAsync(RentalProcessInstance process, RecordPickupInput input, CancellationToken ct)
    {
        return BlankActionResult.Ok(this.ActionType.ToString());
    }

    /// <inheritdoc />
    protected override Task<BlankActionResult> ExecuteAsync(
        RentalProcessInstance process, RecordPickupInput input, CancellationToken ct)
    {
        if (input.Notes is not null && process.Rental is not null)
        {
            process.Rental.Notes = string.IsNullOrEmpty(process.Rental.Notes)
                ? $"[Pickup] {input.Notes}"
                : $"{process.Rental.Notes}\n[Pickup] {input.Notes}";
        }

        return Task.FromResult(BlankActionResult.Ok(nameof(RentalActionType.RecordPickup), RentalStage.PickedUp));
    }
}
