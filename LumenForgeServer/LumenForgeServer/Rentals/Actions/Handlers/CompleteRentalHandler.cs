using System.Text.Json;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Domain.Actions;
using NodaTime;

namespace LumenForgeServer.Rentals.Actions.Handlers;

/// <summary>
/// Completes a rental. Status → Completed.
/// Precondition: Returned.
/// </summary>
public sealed class CompleteRentalHandler : IRentalActionHandler
{
    public RentalActionType ActionType => RentalActionType.CompleteRental;

    public bool CanExecute(Rental rental)
        => rental.RentalStatus == RentalStatus.Returned;

    public Task<RentalAction> ExecuteAsync(Rental rental, JsonElement? input, string actorUserId, CancellationToken ct)
    {
        var now = SystemClock.Instance.GetCurrentInstant();

        rental.RentalStatus = RentalStatus.Completed;
        rental.CompletedAt ??= now;
        rental.Assignment.CompletedByUserId ??= actorUserId;
        rental.UpdatedAt = now;

        var action = new CompleteRentalAction
        {
            Uuid = Guid.NewGuid(),
            RentalId = rental.Id,
            ActionType = RentalActionType.CompleteRental,
            PerformedByUserId = actorUserId,
            ExecutedAt = now,
            CreatedAt = now,
        };

        return Task.FromResult<RentalAction>(action);
    }
}
