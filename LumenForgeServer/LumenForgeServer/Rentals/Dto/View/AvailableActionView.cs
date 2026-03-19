using System.Text.Json.Serialization;
using LumenForgeServer.Rentals.Domain;

namespace LumenForgeServer.Rentals.Dto.View;

/// <summary>
/// Describes an action that is currently available for execution on a rental.
/// </summary>
public sealed record AvailableActionView
{
    [JsonPropertyName("action_type")]
    public ActionType ActionType { get; init; }
}
