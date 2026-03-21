using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Persistence;

namespace LumenForgeServer.Rentals.Service.Actions.Handlers;

/// <summary>Input for the <see cref="GenerateReportHandler"/>.</summary>
public sealed class GenerateReportInput : ActionInput
{
    /// <summary>Whether to include damage details in the report.</summary>
    public bool IncludeDamages { get; init; } = true;

    /// <summary>Whether to include payment details in the report.</summary>
    public bool IncludePayments { get; init; } = true;
}

/// <summary>Extended result that carries the generated report summary.</summary>
public sealed class GenerateReportResult : ActionResult
{
    /// <summary>Summary data for the report.</summary>
    public required RentalReportSummary Summary { get; init; }
}

/// <summary>Summary data returned by the report generator.</summary>
public sealed class RentalReportSummary
{
    public Guid ProcessGuid { get; init; }
    public string? CustomerName { get; init; }
    public string Stage { get; init; } = null!;
    public int DamageCount { get; init; }
    public int ExtensionCount { get; init; }
}

/// <summary>
/// Generates a summary report for the rental. Does not change the process stage.
/// Can be executed in <see cref="RentalStage.Paid"/>, <see cref="RentalStage.Completed"/>,
/// or <see cref="RentalStage.Scrapped"/>.
/// </summary>
public sealed class GenerateReportHandler(IRentalProcessRepository repository)
    : RentalActionHandlerBase<GenerateReportInput>
{
    /// <inheritdoc />
    public override RentalActionType ActionType => RentalActionType.GenerateReport;

    /// <inheritdoc />
    public override IReadOnlySet<RentalStage> AllowedStages { get; } =
        new HashSet<RentalStage> { RentalStage.Paid, RentalStage.Completed, RentalStage.Scrapped };

    /// <inheritdoc />
    protected override async Task<ActionResult> ExecuteAsync(
        RentalProcessInstance process, GenerateReportInput input, CancellationToken ct)
    {
        var detailed = await repository.GetByGuidWithDetailsAsync(process.Guid, ct);

        var summary = new RentalReportSummary
        {
            ProcessGuid = process.Guid,
            CustomerName = detailed?.Rental?.CustomerName,
            Stage = process.CurrentStage.ToString(),
            DamageCount = input.IncludeDamages ? (detailed?.DamageReports.Count ?? 0) : 0,
            ExtensionCount = detailed?.Extensions.Count ?? 0
        };

        return new GenerateReportResult
        {
            Success = true,
            ActionName = nameof(RentalActionType.GenerateReport),
            Summary = summary
        };
    }
}
