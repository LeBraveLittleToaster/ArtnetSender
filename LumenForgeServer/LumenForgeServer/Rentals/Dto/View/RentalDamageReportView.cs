using LumenForgeServer.Common;
using LumenForgeServer.Rentals.Domain;
using NodaTime;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Rentals.Dto.View;

/// <summary>
/// View model for a damage report recorded during post-return inspection.
/// </summary>
public sealed record RentalDamageReportView
{
    [JsonPropertyName("guid")]
    public Guid Guid { get; init; }

    [JsonPropertyName("stock_binding_guid")]
    public Guid StockBindingGuid { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("severity")]
    public DamageSeverity Severity { get; init; }

    [JsonPropertyName("reported_by_kc_id")]
    public required string ReportedByKcId { get; init; }

    [JsonPropertyName("reported_at")]
    public Instant ReportedAt { get; init; }

    public static RentalDamageReportView FromEntity(RentalDamageReport report) => new()
    {
        Guid = report.Guid,
        StockBindingGuid = report.StockBindingGuid,
        Description = report.Description,
        Severity = report.Severity,
        ReportedByKcId = report.ReportedByKcId,
        ReportedAt = report.ReportedAt
    };
}
