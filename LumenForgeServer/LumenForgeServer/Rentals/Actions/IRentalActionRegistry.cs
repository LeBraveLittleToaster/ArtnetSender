using LumenForgeServer.Rentals.Domain;

namespace LumenForgeServer.Rentals.Actions;

/// <summary>
/// Provides the set of actions available for a given <see cref="RentalStage"/>.
/// This is the "implicit process definition" — there is no formal process notation;
/// instead, the valid transitions are encoded in the registry implementation.
/// </summary>
/// <remarks>
/// Used by the <see cref="RentalActionService"/> to answer "what can I do next?"
/// queries and by the controller to expose available actions to the caller.
/// </remarks>
public interface IRentalActionRegistry
{
    /// <summary>
    /// Returns the action types that are valid when the process is in
    /// <paramref name="stage"/>.
    /// </summary>
    IReadOnlySet<RentalActionType> GetAvailableActions(RentalStage stage);
}
