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

    protected override async Task AfterExecuteAsync(RentalProcessInstance process, BlankActionResult result, CancellationToken ct)
    {
        
    }

    protected override async Task<BlankActionResult> BeforeExecuteAsync(RentalProcessInstance process, ScanChecklistInput input, CancellationToken ct)
    {
        return BlankActionResult.Ok(this.ActionType.ToString());
    }

    /// <inheritdoc />
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
