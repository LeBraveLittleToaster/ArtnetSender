using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Inventory.Dto.Create;

/// <summary>
/// Payload for a single device parameter.
/// </summary>
public record CreateDeviceParameterDto
{
    /// <summary>
    /// Parameter key name.
    /// </summary>
    [Required]
    [StringLength(256, MinimumLength = 1)]
    [RegularExpression(@".*\S.*")]
    [JsonPropertyName("key")]
    public required string Key { get; set; }
    /// <summary>
    /// Parameter value.
    /// </summary>
    [Required]
    [StringLength(4000, MinimumLength = 1)]
    [RegularExpression(@".*\S.*")]
    [JsonPropertyName("value")]
    public required string Value { get; set; }
}
