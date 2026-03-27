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

    protected override async Task AfterExecuteAsync(RentalProcessInstance process, BlankActionResult result, CancellationToken ct)
    {
       
    }

    /// <inheritdoc />
    protected override async Task<BlankActionResult> BeforeExecuteAsync(
        RentalProcessInstance process, RemoveItemsInput input, CancellationToken ct)
    {
        if (input.StockBindingGuids.Count == 0)
            return BlankActionResult.Fail(nameof(RentalActionType.RemoveItems), "StockBindingGuids",
                "At least one stock binding GUID is required.");

        return BlankActionResult.Ok(nameof(RentalActionType.RemoveItems));
    }

    /// <inheritdoc />
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
