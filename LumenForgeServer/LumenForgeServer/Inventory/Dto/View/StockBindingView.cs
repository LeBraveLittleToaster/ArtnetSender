using LumenForgeServer.Inventory.Domain;
using NodaTime;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Inventory.Dto.View;

/// <summary>
/// View model for a stock-binding relation tied to a device.
/// </summary>
public sealed record StockBindingView
{
    /// <summary>Unique stock-binding identifier.</summary>
    [JsonPropertyName("guid")]
    public Guid Guid { get; init; }

    /// <summary>Binding type (e.g. RENTAL, MAINTENANCE).</summary>
    [JsonPropertyName("binding_type")]
    public BindingType BindingType { get; init; }

    /// <summary>Start of the reservation period.</summary>
    [JsonPropertyName("start")]
    public Instant Start { get; init; }

    /// <summary>End of the reservation period.</summary>
    [JsonPropertyName("end")]
    public Instant End { get; init; }

    /// <summary>Timestamp when the binding was created.</summary>
    [JsonPropertyName("created_at")]
    public Instant CreatedAt { get; init; }

    public static StockBindingView FromEntity(StockBinding stockBinding)
    {
        return new StockBindingView
        {
            Guid = stockBinding.Guid,
            BindingType = stockBinding.BindingType,
            Start = stockBinding.Start,
            End = stockBinding.End,
            CreatedAt = stockBinding.CreatedAt
        };
    }
}
