using System.Text.Json.Serialization;
using LumenForgeServer.Rentals.Domain;

namespace LumenForgeServer.Rentals.Dto.View;

/// <summary>
/// View model for a rental answer.
/// </summary>
public sealed record AnswerView
{
    [JsonPropertyName("guid")]
    public Guid Guid { get; init; }

    [JsonPropertyName("question_guid")]
    public Guid QuestionGuid { get; init; }

    [JsonPropertyName("question_text")]
    public required string QuestionText { get; init; }

    [JsonPropertyName("value")]
    public required string Value { get; init; }

    /// <summary>
    /// Executes the from entity operation.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="answer">Input value used by this operation.</param>
    /// <returns>The operation result.</returns>
    public static AnswerView FromEntity(Answer answer) => new()
    {
        Guid = answer.Guid,
        QuestionGuid = answer.Question?.Guid ?? Guid.Empty,
        QuestionText = answer.Question?.QuestionText ?? string.Empty,
        Value = answer.Value
    };
}
