using System.Text.Json.Serialization;
using LumenForgeServer.Rentals.Service.Actions.Handlers;

namespace LumenForgeServer.Rentals.Dto.Command;

/// <summary>API request DTO for recording a pickup.</summary>
public sealed record RecordPickupDto
{
    /// <summary>Optional notes about the pickup (e.g. condition remarks).</summary>
    [JsonPropertyName("notes")]
    public string? Notes { get; init; }

    /// <summary>Maps this API DTO to the internal action input.</summary>
    public RecordPickupInput ToActionInput() => new() { Notes = Notes };
}

/// <summary>API request DTO for recording a return.</summary>
public sealed record RecordReturnDto
{
    /// <summary>Optional notes about the return (e.g. condition remarks).</summary>
    [JsonPropertyName("notes")]
    public string? Notes { get; init; }

    /// <summary>Maps this API DTO to the internal action input.</summary>
    public RecordReturnInput ToActionInput() => new() { Notes = Notes };
}
