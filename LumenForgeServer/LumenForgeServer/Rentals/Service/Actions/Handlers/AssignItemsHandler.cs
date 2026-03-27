using LumenForgeServer.Inventory.Domain;
using LumenForgeServer.Inventory.Dto.Create;
using LumenForgeServer.Inventory.Service;
using LumenForgeServer.Rentals.Domain;
using NodaTime.Text;

namespace LumenForgeServer.Rentals.Service.Actions.Handlers;

/// <summary>Input for the <see cref="AssignItemsHandler"/>.</summary>
public sealed class AssignItemsInput : ActionInput
{
    /// <summary>Device GUIDs and quantities to assign (create stock bindings for).</summary>
    public required List<ItemAssignment> Items { get; init; }
}

/// <summary>A single item assignment request.</summary>
public sealed class ItemAssignment
{
    /// <summary>GUID of the inventory device to assign.</summary>
    public required Guid DeviceGuid { get; init; }

    /// <summary>Number of units to reserve.</summary>
    public required long Quantity { get; init; }
}

/// <summary>
/// Assigns inventory items to the rental by creating
/// <c>StockBinding</c> objects from the Inventory module and linking them
/// to the process. Internal action — no customer interaction.
/// </summary>
public sealed class AssignItemsHandler(StockBindingService stockBindingService)
    : RentalActionHandlerBase<AssignItemsInput, BlankActionResult>
{
    /// <inheritdoc />
    public override RentalActionType ActionType => RentalActionType.AssignItems;

    /// <inheritdoc />
    public override IReadOnlySet<RentalStage> AllowedStages { get; } =
        new HashSet<RentalStage> { RentalStage.Approved, RentalStage.ItemsAssigned };

    protected override Task AfterExecuteAsync(RentalProcessInstance process, BlankActionResult result, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override async Task<BlankActionResult> BeforeExecuteAsync(
        RentalProcessInstance process, AssignItemsInput input, CancellationToken ct)
    {
        if (input.Items.Count == 0)
            return BlankActionResult.Fail(nameof(RentalActionType.AssignItems), "Items",
                "At least one item assignment is required.");

        if (input.Items.Any(i => i.Quantity <= 0))
            return BlankActionResult.Fail(nameof(RentalActionType.AssignItems), "Items",
                "Every assignment quantity must be greater than zero.");

        if (process.Rental is null)
            return BlankActionResult.Fail(nameof(RentalActionType.AssignItems), "Rental",
                "Process has no linked rental.");

        return BlankActionResult.Ok(this.ActionType.ToString());
    }

    /// <inheritdoc />
    protected override async Task<BlankActionResult> ExecuteAsync(
        RentalProcessInstance process, AssignItemsInput input, CancellationToken ct)
    {
        var rental = process.Rental!;
        var startStr = InstantPattern.General.Format(rental.RequestedStart);
        var endStr = InstantPattern.General.Format(rental.RequestedEnd);

        var dto = new CreateStockBindingDto
        {
            BindingType = BindingType.RENTAL,
            Start = startStr,
            End = endStr,
            OwnerProcessGuid = process.Guid
        };

        var assignments = input.Items
            .Select(i => new StockBindingAssignment
            {
                DeviceGuid = i.DeviceGuid,
                ReservedAmount = i.Quantity
            })
            .ToList();

        await stockBindingService.CreateStockBindingsForAssignments(assignments, dto, ct);

        return BlankActionResult.Ok(nameof(RentalActionType.AssignItems), RentalStage.ItemsAssigned);
    }
}
