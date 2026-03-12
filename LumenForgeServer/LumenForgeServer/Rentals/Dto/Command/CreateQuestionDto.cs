using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Rentals.Dto.Command;

/// <summary>
/// Payload for creating a survey question.
/// </summary>
public sealed record CreateQuestionDto
{
    [Required]
    [StringLength(500, MinimumLength = 5)]
    [JsonPropertyName("question_text")]
    public required string QuestionText { get; init; }

    [StringLength(64)]
    [JsonPropertyName("category")]
    public string? Category { get; init; }

    [Range(0, int.MaxValue)]
    [JsonPropertyName("display_order")]
    public int DisplayOrder { get; init; } = 0;

    [JsonPropertyName("is_active")]
    public bool IsActive { get; init; } = true;
}
