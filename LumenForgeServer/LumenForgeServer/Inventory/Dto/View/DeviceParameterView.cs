using LumenForgeServer.Inventory.Domain;
using NodaTime;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Inventory.Dto.View;

/// <summary>
/// View model for device key/value parameters.
/// </summary>
public sealed record DeviceParameterView
{
    [JsonPropertyName("key")]
    public required string Key { get; init; }

    [JsonPropertyName("value")]
    public required string Value { get; init; }

    [JsonPropertyName("created_at")]
    public Instant CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public Instant UpdatedAt { get; init; }

    public static DeviceParameterView FromEntity(DeviceParameter parameter)
    {
        return new DeviceParameterView
        {
            Key = parameter.ParamKey,
            Value = parameter.Value,
            CreatedAt = parameter.CreatedAt,
            UpdatedAt = parameter.UpdatedAt
        };
    }
}
