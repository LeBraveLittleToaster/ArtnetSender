using LumenForgeServer.Rentals.Domain;

namespace LumenForgeServer.Rentals.Service.Actions;

/// <summary>
/// Generic base class that concrete action handlers should extend.
/// Bridges the strongly-typed <typeparamref name="TInput"/> with action-specific
/// <typeparamref name="TOutput"/> (a subclass of <see cref="ActionResult"/>) and
/// the non-generic <see cref="IRentalActionHandler{TOutput}"/> interface consumed 
/// by the orchestrator.
/// </summary>
/// <typeparam name="TInput">
/// The action-specific input type. The controller deserialises the request body
/// into this type and passes it through the orchestrator.
/// </typeparam>
/// <typeparam name="TOutput">
/// The action-specific output type. Must be a subclass of <see cref="ActionResult"/>.
/// Use <see cref="BlankActionResult"/> for simple transitions, or create a custom
/// subclass to carry domain data (e.g., newly created GUIDs).
/// </typeparam>
/// <remarks>
/// <para>
/// Subclasses override <see cref="BeforeExecuteAsync"/>, <see cref="ExecuteAsync"/>,
/// and optionally <see cref="AfterExecuteAsync"/> with typed inputs and outputs.
/// The base class implements the <see cref="IRentalActionHandler{TOutput}"/> interface 
/// by casting the <see cref="ActionInput"/> to <typeparamref name="TInput"/>.
/// </para>
/// <para>
/// If the cast fails the orchestrator will receive an
/// <see cref="InvalidCastException"/>, indicating a wiring bug (wrong input type
/// routed to the handler). This is intentional — fail fast in development.
/// </para>
/// </remarks>
public abstract class RentalActionHandlerBase<TInput, TOutput> : IRentalActionHandler
    where TInput : ActionInput
    where TOutput : ActionResult, new()
{
    /// <inheritdoc />
    public abstract RentalActionType ActionType { get; }

    /// <inheritdoc />
    public abstract IReadOnlySet<RentalStage> AllowedStages { get; }

    protected abstract Task<BlankActionResult> BeforeExecuteAsync(RentalProcessInstance process, TInput input, CancellationToken ct);
    
    protected abstract Task<TOutput> ExecuteAsync(RentalProcessInstance process, TInput input, CancellationToken ct);
    protected abstract Task AfterExecuteAsync(RentalProcessInstance process, TOutput result, CancellationToken ct);
    
    

    async Task<BlankActionResult> IRentalActionHandler.BeforeExecuteAsync(RentalProcessInstance process, ActionInput input, CancellationToken ct)
    {
        if (input is not TInput typedInput)
            throw new InvalidCastException($"Expected input of type {typeof(TInput).Name} but got {input.GetType().Name}.");
        return await BeforeExecuteAsync(process, typedInput, ct);
    }

    async Task<ActionResult> IRentalActionHandler.ExecuteAsync(RentalProcessInstance process, ActionInput input, CancellationToken ct)
    {
        if (input is not TInput typedInput)
            throw new InvalidCastException($"Expected input of type {typeof(TInput).Name} but got {input.GetType().Name}.");
        return await ExecuteAsync(process, typedInput, ct);
    }
    async Task IRentalActionHandler.AfterExecuteAsync(RentalProcessInstance process, ActionResult result, CancellationToken ct)
    {
        if (result is not TOutput typedResult)
            throw new InvalidCastException($"Expected result of type {typeof(TOutput).Name} but got {result.GetType().Name}.");
        await AfterExecuteAsync(process, typedResult, ct);
    }
}
