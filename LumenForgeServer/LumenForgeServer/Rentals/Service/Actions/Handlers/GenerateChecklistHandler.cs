using LumenForgeServer.Common;
using LumenForgeServer.Common.Database;
using LumenForgeServer.Inventory.Domain;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Persistence;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace LumenForgeServer.Rentals.Service.Actions.Handlers;

/// <summary>Input for the <see cref="GenerateChecklistHandler"/>.</summary>
public sealed class GenerateChecklistInput : ActionInput
{
    /// <summary>Type of checklist to generate (pickup or dropoff).</summary>
    public required ChecklistType ChecklistType { get; init; }
}

/// <summary>Extended result that carries the newly created checklist GUID.</summary>
public sealed class GenerateChecklistResult : ActionResult
{
    /// <summary>GUID of the generated checklist.</summary>
    public Guid ChecklistGuid { get; init; } = Guid.Empty;
}

/// <summary>
/// Generates a pickup or dropoff checklist for the rental based on the
/// approved item list. Transitions to <see cref="RentalStage.ReadyForPickup"/>.
/// </summary>
public sealed class GenerateChecklistHandler(
    IRentalProcessRepository repository,
    AppDbContext db)
    : RentalActionHandlerBase<GenerateChecklistInput, GenerateChecklistResult>
{
    /// <inheritdoc />
    public override RentalActionType ActionType => RentalActionType.GenerateChecklist;

    /// <inheritdoc />
    public override IReadOnlySet<RentalStage> AllowedStages { get; } =
        new HashSet<RentalStage> { RentalStage.ItemsApproved };

    protected override async Task AfterExecuteAsync(RentalProcessInstance process, GenerateChecklistResult result, CancellationToken ct)
    {
        
    }

    /// <inheritdoc />
    protected override async  Task<BlankActionResult> BeforeExecuteAsync(
        RentalProcessInstance process, GenerateChecklistInput input, CancellationToken ct)
    {
        if (process.Rental is null)
            return new BlankActionResult()
            {
                Success = false,
                ActionName = nameof(RentalActionType.GenerateChecklist),
                Errors = new() { ["Rental"] = "Process has no linked rental." }
            };

        return new BlankActionResult()
        {
            Success = true,
            ActionName = nameof(RentalActionType.GenerateChecklist)
        };
    }

    /// <inheritdoc />
    protected override async Task<GenerateChecklistResult> ExecuteAsync(
        RentalProcessInstance process, GenerateChecklistInput input, CancellationToken ct)
    {
        var rental = process.Rental!;
        var now = SystemClock.Instance.GetCurrentInstant();

        var bindings = await db.StockBindings
            .Include(sb => sb.Device)
            .Where(sb => sb.BindingType == BindingType.RENTAL
                && sb.Start == rental.RequestedStart
                && sb.End == rental.RequestedEnd)
            .ToListAsync(ct);

        var checklist = new Checklist
        {
            Guid = Guid.NewGuid(),
            ProcessInstanceId = process.Id,
            ChecklistType = input.ChecklistType,
            CreatedAt = now,
            Items = bindings.Select(b => new ChecklistItem
            {
                Guid = Guid.NewGuid(),
                StockBindingGuid = b.Guid,
                DeviceName = b.Device?.DeviceName ?? "Unknown",
                IsScanned = false
            }).ToList()
        };

        await repository.AddChecklistAsync(checklist, ct);

        return new GenerateChecklistResult
        {
            Success = true,
            ActionName = nameof(RentalActionType.GenerateChecklist),
            NewStage = RentalStage.ReadyForPickup,
            ChecklistGuid = checklist.Guid
        };
    }
}
