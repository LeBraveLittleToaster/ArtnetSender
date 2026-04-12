using LumenForgeServer.Inventory.Service;
using LumenForgeServer.Rentals.Domain;

namespace LumenForgeServer.Rentals.Service.Actions.Handlers;

/// <summary>Input for the <see cref="RemoveItemsHandler"/>.</summary>
public sealed class RemoveItemsInput : ActionInput
{
    /// <summary>GUIDs of the stock bindings to release.</summary>
    public required List<Guid> StockBindingGuids { get; init; }
}

/// <summary>
/// Removes previously assigned inventory items from the rental,
/// releasing their <c>StockBinding</c> reservations.
/// </summary>
public sealed class RemoveItemsHandler(StockBindingService stockBindingService)
    : RentalActionHandlerBase<RemoveItemsInput, BlankActionResult>
{
    /// <inheritdoc />
    public override RentalActionType ActionType => RentalActionType.RemoveItems;

    /// <inheritdoc />
    public override IReadOnlySet<RentalStage> AllowedStages { get; } =
        new HashSet<RentalStage> { RentalStage.ItemsAssigned };

    /// <summary>
    /// Executes the after execute async operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: may persist state changes, emit workflow logs, or call external dependencies.</remarks>
    /// <param name="process">Input value used by this operation.</param>
    /// <param name="result">Input value used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    protected override async Task AfterExecuteAsync(RentalProcessInstance process, BlankActionResult result, CancellationToken ct)
    {
       
    }

    /// <inheritdoc />
    /// <summary>
    /// Executes the before execute async operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="process">Input value used by this operation.</param>
    /// <param name="input">Request payload containing the input data required for the operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the BlankActionResult result.</returns>
    protected override async Task<BlankActionResult> BeforeExecuteAsync(
        RentalProcessInstance process, RemoveItemsInput input, CancellationToken ct)
    {
        if (input.StockBindingGuids.Count == 0)
            return BlankActionResult.Fail(nameof(RentalActionType.RemoveItems), "StockBindingGuids",
                "At least one stock binding GUID is required.");

        return BlankActionResult.Ok(nameof(RentalActionType.RemoveItems));
    }

    /// <inheritdoc />
    /// <summary>
    /// Executes the execute async operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: may persist state changes, emit workflow logs, or call external dependencies.</remarks>
    /// <param name="process">Input value used by this operation.</param>
    /// <param name="input">Request payload containing the input data required for the operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the BlankActionResult result.</returns>
    protected override async Task<BlankActionResult> ExecuteAsync(
        RentalProcessInstance process, RemoveItemsInput input, CancellationToken ct)
    {
        foreach (var bindingGuid in input.StockBindingGuids)
        {
            await stockBindingService.DeleteStockBindingForOwner(bindingGuid, process.Guid, ct);
        }

        return BlankActionResult.Ok(nameof(RentalActionType.RemoveItems));
    }
}
