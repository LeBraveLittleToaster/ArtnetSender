using LumenForgeServer.Common.Exceptions;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Persistence;
using NodaTime;

namespace LumenForgeServer.Rentals.Service.Actions.Handlers;

/// <summary>Input for the <see cref="SignChecklistHandler"/>.</summary>
public sealed class SignChecklistInput : ActionInput
{
    /// <summary>GUID of the checklist being signed.</summary>
    public required Guid ChecklistGuid { get; init; }

    /// <summary>Base64-encoded signature image or a textual acknowledgement.</summary>
    public required string SignatureData { get; init; }
}

/// <summary>
/// Records a customer or staff signature on a checklist, finalising it.
/// External action — typically triggered after the customer physically signs.
/// </summary>
public sealed class SignChecklistHandler(IRentalProcessRepository repository)
    : RentalActionHandlerBase<SignChecklistInput>
{
    /// <inheritdoc />
    public override RentalActionType ActionType => RentalActionType.SignChecklist;

    /// <inheritdoc />
    public override IReadOnlySet<RentalStage> AllowedStages { get; } =
        new HashSet<RentalStage> { RentalStage.ReadyForPickup };

    /// <inheritdoc />
    protected override async Task<ActionResult> ExecuteAsync(
        RentalProcessInstance process, SignChecklistInput input, CancellationToken ct)
    {
        var checklist = await repository.GetChecklistByGuidAsync(input.ChecklistGuid, ct)
            ?? throw new NotFoundException($"Checklist '{input.ChecklistGuid}' not found.");

        if (checklist.IsSigned)
            return ActionResult.Fail(nameof(RentalActionType.SignChecklist), "Checklist",
                "Checklist has already been signed.");

        var unscanned = checklist.Items.Where(i => !i.IsScanned).ToList();
        if (unscanned.Count > 0)
            return ActionResult.Fail(nameof(RentalActionType.SignChecklist), "Items",
                $"{unscanned.Count} item(s) have not been scanned yet.");

        checklist.IsSigned = true;
        checklist.SignedByKcId = input.ActorKcId;
        checklist.SignatureData = input.SignatureData;
        checklist.SignedAt = SystemClock.Instance.GetCurrentInstant();

        return ActionResult.Ok(nameof(RentalActionType.SignChecklist));
    }
}
