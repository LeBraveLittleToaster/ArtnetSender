using LumenForgeServer.Inventory.Domain;
using NodaTime;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Rentals.Dto.View;

/// <summary>
/// Read model for a stock binding that conflicts with a proposed rental window.
/// Includes the device summary so callers can identify which unit is blocked.
/// </summary>
public sealed record StockBindingConflictView
{
    [JsonPropertyName("binding_guid")]
    public Guid BindingGuid { get; init; }

    [JsonPropertyName("binding_type")]
    public BindingType BindingType { get; init; }

    [JsonPropertyName("device_guid")]
    public Guid DeviceGuid { get; init; }

    [JsonPropertyName("device_serial_number")]
    public required string DeviceSerialNumber { get; init; }

    [JsonPropertyName("device_name")]
    public string? DeviceName { get; init; }

    [JsonPropertyName("start")]
    public Instant Start { get; init; }

    [JsonPropertyName("end")]
    public Instant End { get; init; }

    [JsonPropertyName("created_at")]
    public Instant CreatedAt { get; init; }

    public static StockBindingConflictView FromEntity(StockBinding sb) => new()
    {
        BindingGuid = sb.Guid,
        BindingType = sb.BindingType,
        DeviceGuid = sb.Device.Guid,
        DeviceSerialNumber = sb.Device.SerialNumber,
        DeviceName = sb.Device.DeviceName,
        Start = sb.Start,
        End = sb.End,
        CreatedAt = sb.CreatedAt,
    };
}
