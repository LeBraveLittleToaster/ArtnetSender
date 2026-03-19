using System.Text.Json.Serialization;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Domain.Actions;
using NodaTime;

namespace LumenForgeServer.Rentals.Dto.View;

/// <summary>
/// Read model for an executed rental action.
/// </summary>
public sealed record RentalActionView
{
    [JsonPropertyName("uuid")]
    public Guid Uuid { get; init; }

    [JsonPropertyName("action_type")]
    public RentalActionType ActionType { get; init; }

    [JsonPropertyName("performed_by_user_id")]
    public string? PerformedByUserId { get; init; }

    [JsonPropertyName("executed_at")]
    public Instant ExecutedAt { get; init; }

    [JsonPropertyName("created_at")]
    public Instant CreatedAt { get; init; }

    /// <summary>Action-specific companion data. Null for actions with no extra columns.</summary>
    [JsonPropertyName("details")]
    public object? Details { get; init; }

    public static RentalActionView FromEntity(RentalAction e) => new()
    {
        Uuid = e.Uuid,
        ActionType = e.ActionType,
        PerformedByUserId = e.PerformedByUserId,
        ExecutedAt = e.ExecutedAt,
        CreatedAt = e.CreatedAt,
        Details = MapDetails(e),
    };

    private static object? MapDetails(RentalAction e) => e switch
    {
        RejectRequestAction a => new { reason = a.Reason },
        CancelRentalAction a => new { reason = a.Reason },
        ScrapRentalAction a => new { reason = a.Reason },
        GenerateChecklistAction a => new { checklist_type = a.ChecklistType, checklist_id = a.ChecklistId },
        ScanChecklistAction a => new { checklist_id = a.ChecklistId },
        SignChecklistAction a => new { checklist_id = a.ChecklistId, notes = a.Notes },
        RequestExtensionAction a => new { extension_id = a.ExtensionId },
        ApproveExtensionAction a => new { extension_id = a.ExtensionId },
        RejectExtensionAction a => new { extension_id = a.ExtensionId, reason = a.Reason },
        _ => null,
    };
}
