using LumenForgeServer.Rentals.Domain;

namespace LumenForgeServer.Rentals.Service.Actions;

/// <summary>
/// Generic base class that concrete action handlers should extend.
/// Bridges the strongly-typed <typeparamref name="TInput"/> with action-specific
/// <typeparamref name="TOutput"/> (a subclass of <see cref="ActionResult"/>) and
/// the non-generic <see cref="IRentalActionHandler"/> interface consumed 
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
/// The base class implements the <see cref="IRentalActionHandler"/> interface 
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

    /// <summary>
    /// Executes the before execute async operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="process">Input value used by this operation.</param>
    /// <param name="input">Request payload containing the input data required for the operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the BlankActionResult result.</returns>
    protected abstract Task<BlankActionResult> BeforeExecuteAsync(RentalProcessInstance process, TInput input, CancellationToken ct);
    
    /// <summary>
    /// Executes the execute async operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: may persist state changes, emit workflow logs, or call external dependencies.</remarks>
    /// <param name="process">Input value used by this operation.</param>
    /// <param name="input">Request payload containing the input data required for the operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the TOutput result.</returns>
    protected abstract Task<TOutput> ExecuteAsync(RentalProcessInstance process, TInput input, CancellationToken ct);
    /// <summary>
    /// Executes the after execute async operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: may persist state changes, emit workflow logs, or call external dependencies.</remarks>
    /// <param name="process">Input value used by this operation.</param>
    /// <param name="result">Input value used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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
