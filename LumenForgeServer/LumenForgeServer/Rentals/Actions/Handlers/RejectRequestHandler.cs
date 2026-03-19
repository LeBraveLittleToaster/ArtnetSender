using System.Text.Json;
using LumenForgeServer.Common.Exceptions;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Domain.Actions;
using NodaTime;

namespace LumenForgeServer.Rentals.Actions.Handlers;

/// <summary>
/// Rejects a rental request. Status → Rejected.
/// Input companion: <c>{ "reason": "..." }</c>.
/// </summary>
public sealed class RejectRequestHandler : IRentalActionHandler
{
    public RentalActionType ActionType => RentalActionType.RejectRequest;

    public bool CanExecute(Rental rental)
        => rental.RentalStatus == RentalStatus.Requested;

    public Task<RentalAction> ExecuteAsync(Rental rental, JsonElement? input, string actorUserId, CancellationToken ct)
    {
        var reason = input?.GetProperty("reason").GetString()
            ?? throw new ValidationException("Reason is required.", new Dictionary<string, string[]>
            {
                ["reason"] = ["A rejection reason must be provided."]
            });

        var now = SystemClock.Instance.GetCurrentInstant();

        rental.RentalStatus = RentalStatus.Rejected;
        rental.UpdatedAt = now;

        var action = new RejectRequestAction
        {
            Uuid = Guid.NewGuid(),
            RentalId = rental.Id,
            ActionType = RentalActionType.RejectRequest,
            PerformedByUserId = actorUserId,
            ExecutedAt = now,
            CreatedAt = now,
            Reason = reason,
        };

        return Task.FromResult<RentalAction>(action);
    }
}
