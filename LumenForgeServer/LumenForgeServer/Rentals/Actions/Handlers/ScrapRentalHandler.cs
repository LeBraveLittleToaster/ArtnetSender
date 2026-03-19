using System.Text.Json;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Domain.Actions;
using NodaTime;

namespace LumenForgeServer.Rentals.Actions.Handlers;

/// <summary>
/// Scraps a rental. Status → Scrapped.
/// Optional input: <c>{ "reason": "..." }</c>.
/// Precondition: PickedUp or Returned.
/// </summary>
public sealed class ScrapRentalHandler : IRentalActionHandler
{
    public RentalActionType ActionType => RentalActionType.ScrapRental;

    public bool CanExecute(Rental rental)
        => rental.RentalStatus is RentalStatus.PickedUp or RentalStatus.Returned;

    public Task<RentalAction> ExecuteAsync(Rental rental, JsonElement? input, string actorUserId, CancellationToken ct)
    {
        var reason = input?.TryGetProperty("reason", out var r) == true ? r.GetString() : null;
        var now = SystemClock.Instance.GetCurrentInstant();

        rental.RentalStatus = RentalStatus.Scrapped;
        rental.Scrap.IsScrapped = true;
        rental.Scrap.ScrappedAt = now;
        rental.Scrap.ScrappedByUserId = actorUserId;
        rental.UpdatedAt = now;

        var action = new ScrapRentalAction
        {
            Uuid = Guid.NewGuid(),
            RentalId = rental.Id,
            ActionType = RentalActionType.ScrapRental,
            PerformedByUserId = actorUserId,
            ExecutedAt = now,
            CreatedAt = now,
            Reason = reason,
        };

        return Task.FromResult<RentalAction>(action);
    }
}
