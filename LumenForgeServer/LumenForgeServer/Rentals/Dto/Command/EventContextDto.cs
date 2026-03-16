using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Rentals.Dto.Command;

/// <summary>
/// Event context supplied by the caller to obtain a tailored list of survey questions.
/// Used as the request body for <c>POST /api/v1/rentals/surveys/questions/recommend</c>.
/// </summary>
public sealed record EventContextDto
{
    [Required]
    [StringLength(512, MinimumLength = 1)]
    [JsonPropertyName("event_name")]
    public required string EventName { get; init; }

    [StringLength(4000)]
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>ISO-8601 instant string.</summary>
    [JsonPropertyName("start")]
    public string? Start { get; init; }

    /// <summary>ISO-8601 instant string.</summary>
    [JsonPropertyName("end")]
    public string? End { get; init; }

    [StringLength(1000)]
    [JsonPropertyName("location")]
    public string? Location { get; init; }
}
