using System.Text.Json;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Domain.Actions;

namespace LumenForgeServer.Rentals.Actions;

/// <summary>
/// Contract for a preprogrammed rental action handler.
/// Each implementation corresponds to exactly one <see cref="RentalActionType"/>
/// and is resolved by <see cref="Service.RentalActionService"/> via DI.
/// </summary>
public interface IRentalActionHandler
{
    /// <summary>The action type this handler processes.</summary>
    RentalActionType ActionType { get; }

    /// <summary>
    /// Returns <c>true</c> when the action can be executed against <paramref name="rental"/>
    /// in its current state. Called by the "available actions" endpoint.
    /// </summary>
    bool CanExecute(Rental rental);

    /// <summary>
    /// Executes the action, mutating <paramref name="rental"/> and producing
    /// the persisted <see cref="RentalAction"/> audit record.
    /// </summary>
    /// <param name="rental">The tracked rental entity (loaded with required includes).</param>
    /// <param name="input">Optional JSON payload with action-specific companion input.</param>
    /// <param name="actorUserId">Keycloak user id of the actor.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The concrete <see cref="RentalAction"/> entity to persist.</returns>
    Task<RentalAction> ExecuteAsync(Rental rental, JsonElement? input, string actorUserId, CancellationToken ct);
}
