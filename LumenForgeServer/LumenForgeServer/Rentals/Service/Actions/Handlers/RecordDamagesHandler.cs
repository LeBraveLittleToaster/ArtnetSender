using LumenForgeServer.Common;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Persistence;
using NodaTime;

namespace LumenForgeServer.Rentals.Service.Actions.Handlers;

/// <summary>A single damage entry reported during inspection.</summary>
public sealed class DamageEntry
{
    /// <summary>GUID of the stock binding (item) that is damaged.</summary>
    public required Guid StockBindingGuid { get; init; }

    /// <summary>Free-text description of the damage.</summary>
    public required string Description { get; init; }

    /// <summary>Severity classification.</summary>
    public required DamageSeverity Severity { get; init; }
}

/// <summary>Input for the <see cref="RecordDamagesHandler"/>.</summary>
public sealed class RecordDamagesInput : ActionInput
{
    /// <summary>One or more damage entries found during post-return inspection.</summary>
    public required List<DamageEntry> Damages { get; init; }
}

/// <summary>
/// Records damages found during the post-return inspection of rental items.
/// Transitions the process to <see cref="RentalStage.Inspected"/>.
/// </summary>
public sealed class RecordDamagesHandler(IRentalProcessRepository repository)
    : RentalActionHandlerBase<RecordDamagesInput, BlankActionResult>
{
    /// <inheritdoc />
    public override RentalActionType ActionType => RentalActionType.RecordDamages;

    /// <inheritdoc />
    public override IReadOnlySet<RentalStage> AllowedStages { get; } =
        new HashSet<RentalStage> { RentalStage.Returned };

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
        RentalProcessInstance process, RecordDamagesInput input, CancellationToken ct)
    {
        if (input.Damages.Count == 0)
            return BlankActionResult.Fail(nameof(RentalActionType.RecordDamages), "Damages",
                "At least one damage entry is required.");

        return BlankActionResult.Ok(nameof(RentalActionType.RecordDamages));
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
        RentalProcessInstance process, RecordDamagesInput input, CancellationToken ct)
    {
        var now = SystemClock.Instance.GetCurrentInstant();

        var reports = input.Damages.Select(d => new RentalDamageReport
        {
            Guid = Guid.NewGuid(),
            ProcessInstanceId = process.Id,
            StockBindingGuid = d.StockBindingGuid,
            Description = d.Description,
            Severity = d.Severity,
            ReportedByKcId = input.ActorKcId,
            ReportedAt = now
        }).ToList();

        await repository.AddDamageReportsAsync(reports, ct);

        return BlankActionResult.Ok(nameof(RentalActionType.RecordDamages), RentalStage.Inspected);
    }
}
