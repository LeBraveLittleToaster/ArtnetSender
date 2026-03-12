using LumenForgeServer.Rentals.Domain;
using NodaTime;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Rentals.Dto.View;

/// <summary>
/// Read model for a survey question.
/// </summary>
public sealed record QuestionView
{
    [JsonPropertyName("uuid")]
    public Guid Uuid { get; init; }

    [JsonPropertyName("question_text")]
    public required string QuestionText { get; init; }

    [JsonPropertyName("category")]
    public string? Category { get; init; }

    [JsonPropertyName("display_order")]
    public int DisplayOrder { get; init; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; init; }

    [JsonPropertyName("created_at")]
    public Instant CreatedAt { get; init; }

    public static QuestionView FromEntity(Question e) => new()
    {
        Uuid = e.Uuid,
        QuestionText = e.QuestionText,
        Category = e.Category,
        DisplayOrder = e.DisplayOrder,
        IsActive = e.IsActive,
        CreatedAt = e.CreatedAt,
    };
}
