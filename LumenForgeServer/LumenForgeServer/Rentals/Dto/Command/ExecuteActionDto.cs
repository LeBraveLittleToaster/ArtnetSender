using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using LumenForgeServer.Rentals.Domain;

namespace LumenForgeServer.Rentals.Dto.Command;

/// <summary>
/// Payload for executing a rental action.
/// </summary>
public sealed record ExecuteActionDto
{
    [Required]
    [JsonPropertyName("action_type")]
    public required ActionType ActionType { get; init; }

    /// <summary>
    /// Optional action-specific input companion. Structure varies by <see cref="ActionType"/>.
    /// </summary>
    [JsonPropertyName("input")]
    public JsonElement? Input { get; init; }
}
