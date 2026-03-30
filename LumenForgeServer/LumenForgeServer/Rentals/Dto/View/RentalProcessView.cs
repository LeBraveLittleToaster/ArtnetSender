using LumenForgeServer.Common;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Dto.Query;
using NodaTime;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Rentals.Dto.View;

/// <summary>
/// View model for a rental process instance including its current stage,
/// linked rental data, and nested sub-entities.
/// </summary>
public sealed record RentalProcessView
{
    [JsonPropertyName("guid")]
    public Guid Guid { get; init; }

    [JsonPropertyName("current_stage")]
    public RentalStage CurrentStage { get; init; }

    [JsonPropertyName("created_by_kc_id")]
    public required string CreatedByKcId { get; init; }

    [JsonPropertyName("created_at")]
    public Instant CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public Instant UpdatedAt { get; init; }

    [JsonPropertyName("rental")]
    public RentalView? Rental { get; init; }

    [JsonPropertyName("checklists")]
    public IReadOnlyList<ChecklistView>? Checklists { get; init; }

    [JsonPropertyName("extensions")]
    public IReadOnlyList<RentalExtensionView>? Extensions { get; init; }

    [JsonPropertyName("damage_reports")]
    public IReadOnlyList<RentalDamageReportView>? DamageReports { get; init; }

    /// <summary>
    /// Creates a view from the entity. When <paramref name="includes"/> is
    /// <see cref="RentalProcessInclude.None"/> (default) only the core process
    /// and rental data are returned; nested collections are <c>null</c> and
    /// omitted from the JSON payload.
    /// </summary>
    public static RentalProcessView FromEntity(
        RentalProcessInstance process,
        RentalProcessInclude includes = RentalProcessInclude.None)
    {
        return new RentalProcessView
        {
            Guid = process.Guid,
            CurrentStage = process.CurrentStage,
            CreatedByKcId = process.CreatedByKcId,
            CreatedAt = process.CreatedAt,
            UpdatedAt = process.UpdatedAt,
            Rental = process.Rental is not null ? RentalView.FromEntity(process.Rental) : null,
            Checklists = includes.HasFlag(RentalProcessInclude.Checklists)
                ? process.Checklists.Select(ChecklistView.FromEntity).ToList()
                : null,
            Extensions = includes.HasFlag(RentalProcessInclude.Extensions)
                ? process.Extensions.Select(RentalExtensionView.FromEntity).ToList()
                : null,
            DamageReports = includes.HasFlag(RentalProcessInclude.DamageReports)
                ? process.DamageReports.Select(RentalDamageReportView.FromEntity).ToList()
                : null
        };
    }

    /// <summary>
    /// Convenience overload preserving backward compatibility with the boolean flag.
    /// </summary>
    public static RentalProcessView FromEntity(RentalProcessInstance process, bool includeDetails) =>
        FromEntity(process, includeDetails ? RentalProcessInclude.All : RentalProcessInclude.None);
}

/// <summary>
/// Compact view for list endpoints — omits nested sub-entities.
/// </summary>
public sealed record RentalProcessSummaryView
{
    [JsonPropertyName("guid")]
    public Guid Guid { get; init; }

    [JsonPropertyName("current_stage")]
    public RentalStage CurrentStage { get; init; }

    [JsonPropertyName("created_by_kc_id")]
    public required string CreatedByKcId { get; init; }

    [JsonPropertyName("customer_name")]
    public string? CustomerName { get; init; }

    [JsonPropertyName("customer_email")]
    public string? CustomerEmail { get; init; }

    [JsonPropertyName("group_guid")]
    public Guid? GroupGuid { get; init; }

    [JsonPropertyName("requested_start")]
    public Instant? RequestedStart { get; init; }

    [JsonPropertyName("requested_end")]
    public Instant? RequestedEnd { get; init; }

    [JsonPropertyName("priority")]
    public RentalPriority? Priority { get; init; }

    [JsonPropertyName("created_at")]
    public Instant CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public Instant UpdatedAt { get; init; }

    public static RentalProcessSummaryView FromEntity(RentalProcessInstance process) => new()
    {
        Guid = process.Guid,
        CurrentStage = process.CurrentStage,
        CreatedByKcId = process.CreatedByKcId,
        CustomerName = process.Rental?.CustomerName,
        CustomerEmail = process.Rental?.CustomerEmail,
        GroupGuid = process.Rental?.GroupGuid,
        RequestedStart = process.Rental?.RequestedStart,
        RequestedEnd = process.Rental?.RequestedEnd,
        Priority = process.Rental?.Priority,
        CreatedAt = process.CreatedAt,
        UpdatedAt = process.UpdatedAt
    };
}
