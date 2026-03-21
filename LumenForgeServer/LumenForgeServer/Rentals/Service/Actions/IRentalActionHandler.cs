using LumenForgeServer.Rentals.Domain;

namespace LumenForgeServer.Rentals.Service.Actions;

/// <summary>
/// Non-generic handler contract used by the DI container and the
/// <see cref="RentalActionService"/> orchestrator to discover and invoke
/// action handlers at runtime.
/// </summary>
/// <remarks>
/// <para>
/// Every concrete handler is registered as
/// <c>IServiceCollection.AddScoped&lt;IRentalActionHandler, THandler&gt;()</c>.
/// The orchestrator receives <c>IEnumerable&lt;IRentalActionHandler&gt;</c>,
/// selects the handler whose <see cref="ActionType"/> matches the request,
/// validates <see cref="AllowedStages"/>, and runs the before / execute / after
/// lifecycle.
/// </para>
/// <para>
/// Implementors should derive from <see cref="RentalActionHandlerBase{TInput}"/>
/// instead of implementing this interface directly, so that input deserialization
/// and the lifecycle skeleton are handled automatically.
/// </para>
/// </remarks>
public interface IRentalActionHandler
{
    /// <summary>The action type this handler is responsible for.</summary>
    RentalActionType ActionType { get; }

    /// <summary>
    /// Stages in which this action is allowed to execute.
    /// The orchestrator checks the current stage of the <see cref="RentalProcessInstance"/>
    /// against this set before invoking the handler.
    /// </summary>
    IReadOnlySet<RentalStage> AllowedStages { get; }

    /// <summary>
    /// Runs pre-execution validation and context loading.
    /// Return a failed <see cref="ActionResult"/> to abort the action.
    /// </summary>
    Task<ActionResult> BeforeExecuteAsync(RentalProcessInstance process, ActionInput input, CancellationToken ct);

    /// <summary>
    /// Performs the core business logic of the action.
    /// </summary>
    Task<ActionResult> ExecuteAsync(RentalProcessInstance process, ActionInput input, CancellationToken ct);

    /// <summary>
    /// Runs post-execution logic (cleanup, notifications, side-effects).
    /// Called regardless of success or failure of <see cref="ExecuteAsync"/>.
    /// </summary>
    Task AfterExecuteAsync(RentalProcessInstance process, ActionResult result, CancellationToken ct);
}
