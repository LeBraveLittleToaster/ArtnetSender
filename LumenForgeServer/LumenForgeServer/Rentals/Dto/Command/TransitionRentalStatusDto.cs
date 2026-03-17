using LumenForgeServer.Rentals.Domain;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Rentals.Dto.Command;

/// <summary>
/// Payload for status transitions on an existing rental.
/// </summary>
public sealed record TransitionRentalStatusDto
{
    [Required]
    [JsonPropertyName("target_status")]
    public required RentalStatus TargetStatus { get; init; }

    [StringLength(2000)]
    [JsonPropertyName("reason")]
    public string? Reason { get; init; }
}
