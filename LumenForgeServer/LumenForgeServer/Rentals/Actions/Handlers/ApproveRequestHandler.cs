using System.Text.Json;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Domain.Actions;
using NodaTime;

namespace LumenForgeServer.Rentals.Actions.Handlers;

/// <summary>
/// Approves a rental request. Status → Approved.
/// No input companion required.
/// </summary>
public sealed class ApproveRequestHandler : IRentalActionHandler
{
    public RentalActionType ActionType => RentalActionType.ApproveRequest;

    public bool CanExecute(Rental rental)
        => rental.RentalStatus == RentalStatus.Requested;

    public Task<RentalAction> ExecuteAsync(Rental rental, JsonElement? input, string actorUserId, CancellationToken ct)
    {
        var now = SystemClock.Instance.GetCurrentInstant();

        rental.RentalStatus = RentalStatus.Approved;
        rental.Assignment.AssignedAt ??= now;
        rental.Assignment.AssignedByUserId ??= actorUserId;
        rental.UpdatedAt = now;

        var action = new ApproveRequestAction
        {
            Uuid = Guid.NewGuid(),
            RentalId = rental.Id,
            ActionType = RentalActionType.ApproveRequest,
            PerformedByUserId = actorUserId,
            ExecutedAt = now,
            CreatedAt = now,
        };

        return Task.FromResult<RentalAction>(action);
    }
}
