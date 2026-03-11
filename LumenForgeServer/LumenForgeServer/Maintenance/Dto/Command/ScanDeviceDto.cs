using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Maintenance.Dto.Command;

/// <summary>
/// Payload for a QR-code device scan action.
/// </summary>
public sealed record ScanDeviceDto
{
    [Required]
    [JsonPropertyName("device_guid")]
    public required Guid DeviceGuid { get; init; }
}
