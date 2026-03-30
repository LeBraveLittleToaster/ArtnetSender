using System.Text.Json.Serialization;
using LumenForgeServer.Rentals.Domain;

namespace LumenForgeServer.Rentals.Dto.View;

/// <summary>
/// View model for a rental Question.
/// </summary>
public sealed record QuestionView
{
    [JsonPropertyName("guid")]
    public Guid Guid { get; init; }

    [JsonPropertyName("text")]
    public required string QuestionText { get; set; }

    /// <summary>Logical category for grouping questions.</summary>
    [JsonPropertyName("category")]
    public string? Category { get; set; }

    /// <summary>Display order within the category.</summary>
    [JsonPropertyName("display_order")]
    public int DisplayOrder { get; set; }
    
    /// <summary>Type of data the response should provide in the serialized string.</summary>
    [JsonPropertyName("question_data_type")]
    public QuestionDataType QuestionDataType { get; set; }

    /// <summary>
    /// Executes the from entity operation.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="log">Input value used by this operation.</param>
    /// <returns>The operation result.</returns>
    public static QuestionView FromEntity(Question log) => new()
    {
        Guid = log.Guid,
        Category =  log.Category,
        QuestionText = log.QuestionText,
        DisplayOrder = log.DisplayOrder,
        QuestionDataType = log.QuestionDataType
    };
}