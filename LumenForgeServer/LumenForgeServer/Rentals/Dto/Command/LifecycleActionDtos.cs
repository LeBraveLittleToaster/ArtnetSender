using System.Text.Json.Serialization;
using LumenForgeServer.Rentals.Service.Actions;
using LumenForgeServer.Rentals.Service.Actions.Handlers;

namespace LumenForgeServer.Rentals.Dto.Command;

/// <summary>API request DTO for completing a rental.</summary>
public sealed record CompleteRentalDto : IActionInputDerivable<CompleteRentalInput>
{
    [JsonPropertyName("comment")]
    public string? Comment { get; init; }

    /// <summary>Maps this API DTO to the internal action input.</summary>
    public CompleteRentalInput ToActionInput() => new() { Comment = Comment };
}

/// <summary>API request DTO for cancelling a rental.</summary>
public sealed record CancelRentalDto : IActionInputDerivable<CancelRentalInput>
{
    [JsonPropertyName("reason")]
    public required string Reason { get; init; }

    /// <summary>Maps this API DTO to the internal action input.</summary>
    public CancelRentalInput ToActionInput() => new() { Reason = Reason };
}

/// <summary>API request DTO for scrapping a rental.</summary>
public sealed record ScrapRentalDto : IActionInputDerivable<ScrapRentalInput>
{
    [JsonPropertyName("reason")]
    public required string Reason { get; init; }

    /// <summary>Maps this API DTO to the internal action input.</summary>
    public ScrapRentalInput ToActionInput() => new() { Reason = Reason };
}
