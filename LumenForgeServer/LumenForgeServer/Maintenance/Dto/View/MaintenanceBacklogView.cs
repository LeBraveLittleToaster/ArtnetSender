using LumenForgeServer.Maintenance.Domain;
using NodaTime;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Maintenance.Dto.View;

/// <summary>
/// Read model for a maintenance backlog entry.
/// </summary>
public sealed record MaintenanceBacklogView
{
    [JsonPropertyName("uuid")]
    public Guid Uuid { get; init; }

    [JsonPropertyName("issue_summary")]
    public required string IssueSummary { get; init; }

    [JsonPropertyName("issue_description")]
    public string? IssueDescription { get; init; }

    [JsonPropertyName("quantity_affected")]
    public decimal QuantityAffected { get; init; }

    [JsonPropertyName("status")]
    public required MaintenanceStatusView Status { get; init; }

    [JsonPropertyName("device_uuid")]
    public Guid? DeviceUuid { get; init; }

    [JsonPropertyName("rental_item_uuid")]
    public Guid? RentalItemUuid { get; init; }

    [JsonPropertyName("checklist_item_uuid")]
    public Guid? ChecklistItemUuid { get; init; }

    [JsonPropertyName("reported_at")]
    public Instant ReportedAt { get; init; }

    [JsonPropertyName("resolved_at")]
    public Instant? ResolvedAt { get; init; }

    [JsonPropertyName("created_at")]
    public Instant CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public Instant UpdatedAt { get; init; }

    public static MaintenanceBacklogView FromEntity(MaintenanceBacklog entity) => new()
    {
        Uuid = entity.Uuid,
        IssueSummary = entity.IssueSummary,
        IssueDescription = entity.IssueDescription,
        QuantityAffected = entity.QuantityAffected,
        Status = MaintenanceStatusView.FromEntity(entity.MaintenanceBacklogStatus),
        DeviceUuid = entity.Device?.Guid,
        RentalItemUuid = entity.RentalItem?.Uuid,
        ChecklistItemUuid = entity.ChecklistItem?.Uuid,
        ReportedAt = entity.ReportedAt,
        ResolvedAt = entity.ResolvedAt,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
    };
}
