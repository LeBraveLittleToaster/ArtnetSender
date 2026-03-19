using System.Text.Json;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Domain.Actions;
using NodaTime;

namespace LumenForgeServer.Rentals.Actions.Handlers;

/// <summary>
/// Records pickup of the rental. Status → PickedUp.
/// Precondition: Approved.
/// </summary>
public sealed class RecordPickupHandler : IRentalActionHandler
{
    public RentalActionType ActionType => RentalActionType.RecordPickup;

    public bool CanExecute(Rental rental)
        => rental.RentalStatus == RentalStatus.Approved;

    public Task<RentalAction> ExecuteAsync(Rental rental, JsonElement? input, string actorUserId, CancellationToken ct)
    {
        var now = SystemClock.Instance.GetCurrentInstant();

        rental.RentalStatus = RentalStatus.PickedUp;
        rental.Schedule.PickupAt ??= now;
        rental.Assignment.PickupProcessedByUserId ??= actorUserId;
        rental.UpdatedAt = now;

        var action = new RecordPickupAction
        {
            Uuid = Guid.NewGuid(),
            RentalId = rental.Id,
            ActionType = RentalActionType.RecordPickup,
            PerformedByUserId = actorUserId,
            ExecutedAt = now,
            CreatedAt = now,
        };

        return Task.FromResult<RentalAction>(action);
    }
}
