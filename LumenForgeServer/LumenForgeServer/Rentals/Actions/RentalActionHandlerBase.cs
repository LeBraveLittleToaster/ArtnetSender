using LumenForgeServer.Rentals.Domain;

namespace LumenForgeServer.Rentals.Actions;

/// <summary>
/// Generic base class that concrete action handlers should extend.
/// Bridges the strongly-typed <typeparamref name="TInput"/> with the non-generic
/// <see cref="IRentalActionHandler"/> interface consumed by the orchestrator.
/// </summary>
/// <typeparam name="TInput">
/// The action-specific input type. The controller deserialises the request body
/// into this type and passes it through the orchestrator.
/// </typeparam>
/// <remarks>
/// <para>
/// Subclasses override <see cref="BeforeExecuteAsync"/>, <see cref="ExecuteAsync"/>,
/// and optionally <see cref="AfterExecuteAsync"/> with typed inputs.
/// The base class implements the <see cref="IRentalActionHandler"/> interface by
/// casting the <see cref="ActionInput"/> to <typeparamref name="TInput"/>.
/// </para>
/// <para>
/// If the cast fails the orchestrator will receive an
/// <see cref="InvalidCastException"/>, indicating a wiring bug (wrong input type
/// routed to the handler). This is intentional — fail fast in development.
/// </para>
/// </remarks>
public abstract class RentalActionHandlerBase<TInput> : IRentalActionHandler
    where TInput : ActionInput
{
    /// <inheritdoc />
    public abstract RentalActionType ActionType { get; }

    /// <inheritdoc />
    public abstract IReadOnlySet<RentalStage> AllowedStages { get; }

    // ── IRentalActionHandler explicit implementation (non-generic bridge) ────

    Task<ActionResult> IRentalActionHandler.BeforeExecuteAsync(
        RentalProcessInstance process, ActionInput input, CancellationToken ct)
        => BeforeExecuteAsync(process, (TInput)input, ct);

    Task<ActionResult> IRentalActionHandler.ExecuteAsync(
        RentalProcessInstance process, ActionInput input, CancellationToken ct)
        => ExecuteAsync(process, (TInput)input, ct);

    Task IRentalActionHandler.AfterExecuteAsync(
        RentalProcessInstance process, ActionResult result, CancellationToken ct)
        => AfterExecuteAsync(process, result, ct);

    // ── Protected typed lifecycle methods for subclasses ─────────────────────

    /// <summary>
    /// Pre-execution hook. Validate preconditions, load additional context, etc.
    /// Return <see cref="ActionResult.Fail(string, string, string)"/> to abort.
    /// </summary>
    protected virtual Task<ActionResult> BeforeExecuteAsync(
        RentalProcessInstance process, TInput input, CancellationToken ct)
        => Task.FromResult(ActionResult.Ok(ActionType.ToString()));

    /// <summary>
    /// Core business logic of the action. Must be overridden by every handler.
    /// </summary>
    protected abstract Task<ActionResult> ExecuteAsync(
        RentalProcessInstance process, TInput input, CancellationToken ct);

    /// <summary>
    /// Post-execution hook. Runs after <see cref="ExecuteAsync"/> regardless of outcome.
    /// Override to send notifications, trigger side-effects, or release resources.
    /// </summary>
    protected virtual Task AfterExecuteAsync(
        RentalProcessInstance process, ActionResult result, CancellationToken ct)
        => Task.CompletedTask;
}
