using System.Text.Json;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Domain.Actions;
using NodaTime;

namespace LumenForgeServer.Rentals.Actions.Handlers;

/// <summary>
/// Records return/dropoff of the rental. Status → Returned.
/// Precondition: PickedUp.
/// </summary>
public sealed class RecordReturnHandler : IRentalActionHandler
{
    public RentalActionType ActionType => RentalActionType.RecordReturn;

    public bool CanExecute(Rental rental)
        => rental.RentalStatus == RentalStatus.PickedUp;

    public Task<RentalAction> ExecuteAsync(Rental rental, JsonElement? input, string actorUserId, CancellationToken ct)
    {
        var now = SystemClock.Instance.GetCurrentInstant();

        rental.RentalStatus = RentalStatus.Returned;
        rental.Schedule.DropoffAt ??= now;
        rental.Assignment.DropoffProcessedByUserId ??= actorUserId;
        rental.UpdatedAt = now;

        var action = new RecordReturnAction
        {
            Uuid = Guid.NewGuid(),
            RentalId = rental.Id,
            ActionType = RentalActionType.RecordReturn,
            PerformedByUserId = actorUserId,
            ExecutedAt = now,
            CreatedAt = now,
        };

        return Task.FromResult<RentalAction>(action);
    }
}
