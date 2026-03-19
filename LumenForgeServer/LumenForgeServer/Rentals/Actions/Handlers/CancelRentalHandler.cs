using System.Text.Json;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Domain.Actions;
using NodaTime;

namespace LumenForgeServer.Rentals.Actions.Handlers;

/// <summary>
/// Cancels a rental. Status → Cancelled.
/// Optional input: <c>{ "reason": "..." }</c>.
/// Precondition: Requested or Approved.
/// </summary>
public sealed class CancelRentalHandler : IRentalActionHandler
{
    public RentalActionType ActionType => RentalActionType.CancelRental;

    public bool CanExecute(Rental rental)
        => rental.RentalStatus is RentalStatus.Requested or RentalStatus.Approved;

    public Task<RentalAction> ExecuteAsync(Rental rental, JsonElement? input, string actorUserId, CancellationToken ct)
    {
        var reason = input?.TryGetProperty("reason", out var r) == true ? r.GetString() : null;
        var now = SystemClock.Instance.GetCurrentInstant();

        rental.RentalStatus = RentalStatus.Cancelled;
        rental.UpdatedAt = now;

        var action = new CancelRentalAction
        {
            Uuid = Guid.NewGuid(),
            RentalId = rental.Id,
            ActionType = RentalActionType.CancelRental,
            PerformedByUserId = actorUserId,
            ExecutedAt = now,
            CreatedAt = now,
            Reason = reason,
        };

        return Task.FromResult<RentalAction>(action);
    }
}
