using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Service.Actions;
using NodaTime;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Rentals.Dto.View;

/// <summary>
/// View model for a rental action audit log entry.
/// </summary>
public sealed record RentalActionLogView
{
    [JsonPropertyName("guid")]
    public Guid Guid { get; init; }

    [JsonPropertyName("action_type")]
    public RentalActionType ActionType { get; init; }

    [JsonPropertyName("performed_by_kc_id")]
    public required string PerformedByKcId { get; init; }

    [JsonPropertyName("stage_before")]
    public RentalStage StageBefore { get; init; }

    [JsonPropertyName("stage_after")]
    public RentalStage StageAfter { get; init; }

    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; init; }

    [JsonPropertyName("performed_at")]
    public Instant PerformedAt { get; init; }

    public static RentalActionLogView FromEntity(RentalActionLog log) => new()
    {
        Guid = log.Guid,
        ActionType = log.ActionType,
        PerformedByKcId = log.PerformedByKcId,
        StageBefore = log.StageBefore,
        StageAfter = log.StageAfter,
        Success = log.Success,
        ErrorMessage = log.ErrorMessage,
        PerformedAt = log.PerformedAt
    };
}
