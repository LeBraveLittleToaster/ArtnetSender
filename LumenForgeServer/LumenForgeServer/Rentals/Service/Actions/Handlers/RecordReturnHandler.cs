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
public sealed class RecordReturnHandler : RentalActionHandlerBase<RecordReturnInput>
{
    /// <inheritdoc />
    public override RentalActionType ActionType => RentalActionType.RecordReturn;

    /// <inheritdoc />
    public override IReadOnlySet<RentalStage> AllowedStages { get; } =
        new HashSet<RentalStage> { RentalStage.PickedUp };

    /// <inheritdoc />
    protected override Task<ActionResult> ExecuteAsync(
        RentalProcessInstance process, RecordReturnInput input, CancellationToken ct)
    {
        if (input.Notes is not null && process.Rental is not null)
        {
            process.Rental.Notes = string.IsNullOrEmpty(process.Rental.Notes)
                ? $"[Return] {input.Notes}"
                : $"{process.Rental.Notes}\n[Return] {input.Notes}";
        }

        return Task.FromResult(ActionResult.Ok(nameof(RentalActionType.RecordReturn), RentalStage.Returned));
    }
}
