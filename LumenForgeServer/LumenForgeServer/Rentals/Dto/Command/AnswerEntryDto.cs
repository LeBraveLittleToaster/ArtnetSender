using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Rentals.Dto.Command;

/// <summary>
/// A single answer entry within a bulk survey submission.
/// </summary>
public sealed record AnswerEntryDto
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
}
