using LumenForgeServer.Rentals.Domain;

namespace LumenForgeServer.Rentals.Actions.Handlers;

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
public sealed class RecordPickupHandler : RentalActionHandlerBase<RecordPickupInput>
{
    /// <inheritdoc />
    public override RentalActionType ActionType => RentalActionType.RecordPickup;

    /// <inheritdoc />
    public override IReadOnlySet<RentalStage> AllowedStages { get; } =
        new HashSet<RentalStage> { RentalStage.ReadyForPickup };

    /// <inheritdoc />
    protected override Task<ActionResult> ExecuteAsync(
        RentalProcessInstance process, RecordPickupInput input, CancellationToken ct)
    {
        if (input.Notes is not null && process.Rental is not null)
        {
            process.Rental.Notes = string.IsNullOrEmpty(process.Rental.Notes)
                ? $"[Pickup] {input.Notes}"
                : $"{process.Rental.Notes}\n[Pickup] {input.Notes}";
        }

        return Task.FromResult(ActionResult.Ok(nameof(RentalActionType.RecordPickup), RentalStage.PickedUp));
    }
}
