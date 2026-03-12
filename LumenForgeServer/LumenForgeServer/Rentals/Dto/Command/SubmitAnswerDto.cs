using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Rentals.Dto.Command;

/// <summary>
/// Payload for submitting an answer to a survey question.
/// </summary>
public sealed record SubmitAnswerDto
{
    [Required]
    [JsonPropertyName("question_uuid")]
    public required Guid QuestionUuid { get; init; }

    [Required]
    [RegularExpression("^(Yes|No|NotImportant|Unknown)$")]
    [JsonPropertyName("response")]
    public required string Response { get; init; }

    [StringLength(2000)]
    [JsonPropertyName("comment")]
    public string? Comment { get; init; }

    /// <summary>
    /// Optional rental this answer is associated with.
    /// </summary>
    [JsonPropertyName("rental_uuid")]
    public Guid? RentalUuid { get; init; }
}
