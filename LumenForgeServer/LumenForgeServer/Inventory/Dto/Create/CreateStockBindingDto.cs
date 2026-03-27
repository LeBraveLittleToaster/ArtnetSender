using LumenForgeServer.Inventory.Domain;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Inventory.Dto.Create;

/// <summary>
/// Payload for creating a stock binding.
/// </summary>
public record CreateStockBindingDto
{
    /// <summary>
    /// Binding type (RENTAL, MAINTENANCE, etc.).
    /// </summary>
    [Required]
    [JsonPropertyName("binding_type")]
    public required BindingType BindingType { get; set; }

    /// <summary>
    /// Start of the binding period.
    /// </summary>
    [Required]
    [JsonPropertyName("start")]
    public required string Start { get; set; }

    /// <summary>
    /// End of the binding period.
    /// </summary>
    [Required]
    [JsonPropertyName("end")]
    public required string End { get; set; }

    /// <summary>
    /// Reserved amount in the device's unit space.
    /// </summary>
    [Range(1, long.MaxValue)]
    [JsonPropertyName("reserved_amount")]
    public long ReservedAmount { get; set; } = 1;

    /// <summary>
    /// Optional rental process owner GUID for scoping reservations.
    /// </summary>
    [JsonPropertyName("owner_process_guid")]
    public Guid? OwnerProcessGuid { get; set; }
}
