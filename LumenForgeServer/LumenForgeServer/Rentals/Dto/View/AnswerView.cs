using LumenForgeServer.Rentals.Domain;
using NodaTime;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Rentals.Dto.View;

/// <summary>
/// Read model for a survey answer.
/// </summary>
public sealed record AnswerView
{
    [JsonPropertyName("uuid")]
    public Guid Uuid { get; init; }

    [JsonPropertyName("question_uuid")]
    public Guid QuestionUuid { get; init; }

    [JsonPropertyName("question_text")]
    public required string QuestionText { get; init; }

    [JsonPropertyName("response")]
    public required string Response { get; init; }

    [JsonPropertyName("comment")]
    public string? Comment { get; init; }

    [JsonPropertyName("respondent_user_id")]
    public string? RespondentUserId { get; init; }

    [JsonPropertyName("rental_uuid")]
    public Guid? RentalUuid { get; init; }

    [JsonPropertyName("created_at")]
    public Instant CreatedAt { get; init; }

    public static AnswerView FromEntity(Answer e) => new()
    {
        Uuid = e.Uuid,
        QuestionUuid = e.Question.Uuid,
        QuestionText = e.Question.QuestionText,
        Response = e.Response,
        Comment = e.Comment,
        RespondentUserId = e.RespondentUserId,
        RentalUuid = e.Rental?.Uuid,
        CreatedAt = e.CreatedAt,
    };
}
