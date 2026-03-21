using LumenForgeServer.Rentals.Domain;
using NodaTime;

namespace LumenForgeServer.Rentals.Service.Actions.Handlers;

/// <summary>Input for the <see cref="ScrapRentalHandler"/>.</summary>
public sealed class ScrapRentalInput : ActionInput
{
    /// <summary>Reason for scrapping the rental (e.g. total loss).</summary>
    public required string Reason { get; init; }
}

/// <summary>
/// Scraps the rental — a total write-off of all assigned items.
/// Can only be triggered while items are with the customer
/// (<see cref="RentalStage.PickedUp"/>). Transitions to
/// <see cref="RentalStage.Scrapped"/> — a terminal state.
/// </summary>
public sealed class ScrapRentalHandler : RentalActionHandlerBase<ScrapRentalInput>
{
    /// <inheritdoc />
    public override RentalActionType ActionType => RentalActionType.ScrapRental;

    /// <inheritdoc />
    public override IReadOnlySet<RentalStage> AllowedStages { get; } =
        new HashSet<RentalStage> { RentalStage.PickedUp };

    /// <inheritdoc />
    protected override Task<ActionResult> ExecuteAsync(
        RentalProcessInstance process, ScrapRentalInput input, CancellationToken ct)
    {
        if (process.Rental is not null)
        {
            process.Rental.Notes = string.IsNullOrEmpty(process.Rental.Notes)
                ? $"[Scrapped] {input.Reason}"
                : $"{process.Rental.Notes}\n[Scrapped] {input.Reason}";
            process.Rental.UpdatedAt = SystemClock.Instance.GetCurrentInstant();
        }

        return Task.FromResult(ActionResult.Ok(nameof(RentalActionType.ScrapRental), RentalStage.Scrapped));
    }
}
