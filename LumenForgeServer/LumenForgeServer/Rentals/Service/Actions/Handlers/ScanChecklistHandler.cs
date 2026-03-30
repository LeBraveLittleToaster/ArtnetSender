using LumenForgeServer.Common.Exceptions;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Persistence;
using NodaTime;

namespace LumenForgeServer.Rentals.Service.Actions.Handlers;

/// <summary>Input for the <see cref="ScanChecklistHandler"/>.</summary>
public sealed class ScanChecklistInput : ActionInput
{
    /// <summary>GUID of the checklist being scanned.</summary>
    public required Guid ChecklistGuid { get; init; }

    /// <summary>Serial number or barcode value scanned from the device.</summary>
    public required string ScannedValue { get; init; }
}

/// <summary>
/// Records a device scan against an existing checklist.
/// Does not change the process stage — scanning is an incremental sub-step
/// within the <see cref="RentalStage.ReadyForPickup"/> stage.
/// </summary>
public sealed class ScanChecklistHandler(IRentalProcessRepository repository)
    : RentalActionHandlerBase<ScanChecklistInput, BlankActionResult>
{
    /// <inheritdoc />
    public override RentalActionType ActionType => RentalActionType.ScanChecklist;

    /// <inheritdoc />
    public override IReadOnlySet<RentalStage> AllowedStages { get; } =
        new HashSet<RentalStage> { RentalStage.ReadyForPickup };

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

    /// <summary>
    /// Executes the before execute async operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="process">Input value used by this operation.</param>
    /// <param name="input">Request payload containing the input data required for the operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the BlankActionResult result.</returns>
    protected override async Task<BlankActionResult> BeforeExecuteAsync(RentalProcessInstance process, ScanChecklistInput input, CancellationToken ct)
    {
        return BlankActionResult.Ok(this.ActionType.ToString());
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
        RentalProcessInstance process, ScanChecklistInput input, CancellationToken ct)
    {
        var checklist = await repository.GetChecklistByGuidAsync(input.ChecklistGuid, ct)
            ?? throw new NotFoundException($"Checklist '{input.ChecklistGuid}' not found.");

        var item = checklist.Items.FirstOrDefault(i => !i.IsScanned);

        if (item is null)
            return BlankActionResult.Fail(nameof(RentalActionType.ScanChecklist), "Checklist",
                "All items have already been scanned.");

        item.IsScanned = true;
        item.ScannedValue = input.ScannedValue;
        item.ScannedByKcId = input.ActorKcId;
        item.ScannedAt = SystemClock.Instance.GetCurrentInstant();

        return BlankActionResult.Ok(nameof(RentalActionType.ScanChecklist));
    }
}
