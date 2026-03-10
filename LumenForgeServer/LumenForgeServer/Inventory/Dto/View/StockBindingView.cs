using LumenForgeServer.Inventory.Domain;
using NodaTime;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Inventory.Dto.View;

/// <summary>
/// View model for a stock-binding relation tied to a device.
/// </summary>
public sealed record StockBindingView
{
    [JsonPropertyName("guid")]
    public Guid Guid { get; init; }

    [JsonPropertyName("binding_type")]
    public BindingType BindingType { get; init; }

    [JsonPropertyName("start")]
    public Instant Start { get; init; }

    [JsonPropertyName("end")]
    public Instant End { get; init; }

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
