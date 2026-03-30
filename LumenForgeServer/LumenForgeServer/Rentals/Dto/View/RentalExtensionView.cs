using LumenForgeServer.Rentals.Domain;
using NodaTime;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Rentals.Dto.View;

/// <summary>
/// View model for a rental extension request.
/// </summary>
public sealed record RentalExtensionView
{
    [JsonPropertyName("guid")]
    public Guid Guid { get; init; }

    [JsonPropertyName("new_requested_end")]
    public Instant NewRequestedEnd { get; init; }

    [JsonPropertyName("original_end")]
    public Instant OriginalEnd { get; init; }

    [JsonPropertyName("reason")]
    public string? Reason { get; init; }

    [JsonPropertyName("is_approved")]
    public bool? IsApproved { get; init; }

    [JsonPropertyName("review_comment")]
    public string? ReviewComment { get; init; }

    [JsonPropertyName("requested_by_kc_id")]
    public required string RequestedByKcId { get; init; }

    [JsonPropertyName("reviewed_by_kc_id")]
    public string? ReviewedByKcId { get; init; }

    [JsonPropertyName("requested_at")]
    public Instant RequestedAt { get; init; }

    [JsonPropertyName("reviewed_at")]
    public Instant? ReviewedAt { get; init; }

    /// <summary>
    /// Executes the from entity operation.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="extension">Input value used by this operation.</param>
    /// <returns>The operation result.</returns>
    public static RentalExtensionView FromEntity(RentalExtension extension) => new()
    {
        Guid = extension.Guid,
        NewRequestedEnd = extension.NewRequestedEnd,
        OriginalEnd = extension.OriginalEnd,
        Reason = extension.Reason,
        IsApproved = extension.IsApproved,
        ReviewComment = extension.ReviewComment,
        RequestedByKcId = extension.RequestedByKcId,
        ReviewedByKcId = extension.ReviewedByKcId,
        RequestedAt = extension.RequestedAt,
        ReviewedAt = extension.ReviewedAt
    };
}
