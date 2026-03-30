using System.Text.Json.Serialization;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Service.Actions;
using LumenForgeServer.Rentals.Service.Actions.Handlers;
using NodaTime;

namespace LumenForgeServer.Rentals.Dto.View;

public abstract record ActionResultView
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("action_name")]
    public string ActionName { get; init; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public Instant Timestamp { get; init; }

    [JsonPropertyName("new_stage")]
    public RentalStage? NewStage { get; init; }

    [JsonPropertyName("errors")]
    public Dictionary<string, string> Errors { get; init; } = [];

    /// <summary>
    /// Executes the from action result operation.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="result">Input value used by this operation.</param>
    /// <returns>The operation result.</returns>
    public static ActionResultView FromActionResult(ActionResult result) => result switch
    {
        BlankActionResult blank => BlankActionResultView.FromActionResult(blank),
        CreateRentalResult createRental => CreateRentalResultView.FromActionResult(createRental),
        GenerateChecklistResult checklist => GenerateChecklistResultView.FromActionResult(checklist),
        GenerateInvoiceResult invoice => GenerateInvoiceResultView.FromActionResult(invoice),
        GenerateReportResult report => GenerateReportResultView.FromActionResult(report),
        RequestExtensionResult extension => RequestExtensionResultView.FromActionResult(extension),
        CreateMaintenanceJobsResult jobs => CreateMaintenanceJobsResultView.FromActionResult(jobs),
        _ => throw new InvalidOperationException($"No view DTO mapping found for action result type '{result.GetType().Name}'.")
    };
}

public sealed record BlankActionResultView : ActionResultView
{
    /// <summary>
    /// Executes the from action result operation.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="result">Input value used by this operation.</param>
    /// <returns>The operation result.</returns>
    public static BlankActionResultView FromActionResult(BlankActionResult result) => new()
    {
        Success = result.Success,
        ActionName = result.ActionName,
        Timestamp = result.Timestamp,
        NewStage = result.NewStage,
        Errors = result.Errors
    };
}

public sealed record CreateRentalResultView : ActionResultView
{
    [JsonPropertyName("process_instance_guid")]
    public Guid ProcessInstanceGuid { get; init; }

    /// <summary>
    /// Executes the from action result operation.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="result">Input value used by this operation.</param>
    /// <returns>The operation result.</returns>
    public static CreateRentalResultView FromActionResult(CreateRentalResult result) => new()
    {
        Success = result.Success,
        ActionName = result.ActionName,
        Timestamp = result.Timestamp,
        NewStage = result.NewStage,
        Errors = result.Errors,
        ProcessInstanceGuid = result.ProcessInstanceGuid
    };
}

public sealed record GenerateChecklistResultView : ActionResultView
{
    [JsonPropertyName("checklist_guid")]
    public Guid ChecklistGuid { get; init; }

    /// <summary>
    /// Executes the from action result operation.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="result">Input value used by this operation.</param>
    /// <returns>The operation result.</returns>
    public static GenerateChecklistResultView FromActionResult(GenerateChecklistResult result) => new()
    {
        Success = result.Success,
        ActionName = result.ActionName,
        Timestamp = result.Timestamp,
        NewStage = result.NewStage,
        Errors = result.Errors,
        ChecklistGuid = result.ChecklistGuid
    };
}

public sealed record GenerateInvoiceResultView : ActionResultView
{
    [JsonPropertyName("invoice_guid")]
    public Guid InvoiceGuid { get; init; }

    /// <summary>
    /// Executes the from action result operation.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="result">Input value used by this operation.</param>
    /// <returns>The operation result.</returns>
    public static GenerateInvoiceResultView FromActionResult(GenerateInvoiceResult result) => new()
    {
        Success = result.Success,
        ActionName = result.ActionName,
        Timestamp = result.Timestamp,
        NewStage = result.NewStage,
        Errors = result.Errors,
        InvoiceGuid = result.InvoiceGuid
    };
}

public sealed record RequestExtensionResultView : ActionResultView
{
    [JsonPropertyName("extension_guid")]
    public Guid? ExtensionGuid { get; init; }

    /// <summary>
    /// Executes the from action result operation.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="result">Input value used by this operation.</param>
    /// <returns>The operation result.</returns>
    public static RequestExtensionResultView FromActionResult(RequestExtensionResult result) => new()
    {
        Success = result.Success,
        ActionName = result.ActionName,
        Timestamp = result.Timestamp,
        NewStage = result.NewStage,
        Errors = result.Errors,
        ExtensionGuid = result.ExtensionGuid
    };
}

public sealed record CreateMaintenanceJobsResultView : ActionResultView
{
    [JsonPropertyName("maintenance_job_guids")]
    public Guid[] MaintenanceJobGuids { get; init; } = [];

    /// <summary>
    /// Executes the from action result operation.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="result">Numeric input used by this operation.</param>
    /// <returns>The operation result.</returns>
    public static CreateMaintenanceJobsResultView FromActionResult(CreateMaintenanceJobsResult result) => new()
    {
        Success = result.Success,
        ActionName = result.ActionName,
        Timestamp = result.Timestamp,
        NewStage = result.NewStage,
        Errors = result.Errors,
        MaintenanceJobGuids = result.MaintenanceJobGuids
    };
}

public sealed record RentalReportSummaryView
{
    [JsonPropertyName("process_guid")]
    public Guid ProcessGuid { get; init; }

    [JsonPropertyName("customer_name")]
    public string? CustomerName { get; init; }

    [JsonPropertyName("stage")]
    public string Stage { get; init; } = string.Empty;

    [JsonPropertyName("damage_count")]
    public int DamageCount { get; init; }

    [JsonPropertyName("extension_count")]
    public int ExtensionCount { get; init; }

    /// <summary>
    /// Executes the from summary operation.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="summary">Input value used by this operation.</param>
    /// <returns>The operation result.</returns>
    public static RentalReportSummaryView FromSummary(RentalReportSummary summary) => new()
    {
        ProcessGuid = summary.ProcessGuid,
        CustomerName = summary.CustomerName,
        Stage = summary.Stage,
        DamageCount = summary.DamageCount,
        ExtensionCount = summary.ExtensionCount
    };
}

public sealed record GenerateReportResultView : ActionResultView
{
    [JsonPropertyName("summary")]
    public RentalReportSummaryView? Summary { get; init; }

    /// <summary>
    /// Executes the from action result operation.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="result">Input value used by this operation.</param>
    /// <returns>The operation result.</returns>
    public static GenerateReportResultView FromActionResult(GenerateReportResult result) => new()
    {
        Success = result.Success,
        ActionName = result.ActionName,
        Timestamp = result.Timestamp,
        NewStage = result.NewStage,
        Errors = result.Errors,
        Summary = result.Summary is null ? null : RentalReportSummaryView.FromSummary(result.Summary)
    };
}
