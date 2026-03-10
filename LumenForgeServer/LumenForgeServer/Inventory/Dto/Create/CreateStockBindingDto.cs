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
}
