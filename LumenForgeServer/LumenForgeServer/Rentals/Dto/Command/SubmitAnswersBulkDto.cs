using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Rentals.Dto.Command;

/// <summary>
/// Payload for submitting all survey answers for one rental in a single request.
/// </summary>
public sealed record SubmitAnswersBulkDto
{
    [Required]
    [JsonPropertyName("rental_uuid")]
    public required Guid RentalUuid { get; init; }

    [Required]
    [MinLength(1)]
    [JsonPropertyName("answers")]
    public required IReadOnlyList<AnswerEntryDto> Answers { get; init; }
}
