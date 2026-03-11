using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Rentals.Dto.Command;

/// <summary>
/// Payload for signing and finalising a checklist.
/// A signed checklist is immutable; no further item updates are accepted.
/// </summary>
public sealed record SignChecklistDto
{
    [StringLength(4000)]
    [JsonPropertyName("notes")]
    public string? Notes { get; init; }
}
