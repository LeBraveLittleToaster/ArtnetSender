using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Rentals.Dto.Command;

/// <summary>
/// Payload for submitting an inspection result for a single checklist item.
/// Calling this endpoint marks the item as checked regardless of the quantity value.
/// </summary>
public sealed record UpdateChecklistItemDto
{
    [Required]
    [Range(0, double.MaxValue)]
    [JsonPropertyName("quantity_checked")]
    public required decimal QuantityChecked { get; init; }

    [Required]
    [JsonPropertyName("condition_ok")]
    public required bool ConditionOk { get; init; }

    [StringLength(4000)]
    [JsonPropertyName("condition_notes")]
    public string? ConditionNotes { get; init; }

    [Range(0, double.MaxValue)]
    [JsonPropertyName("damaged_quantity")]
    public decimal DamagedQuantity { get; init; } = 0;

    [StringLength(2000)]
    [JsonPropertyName("damage_summary")]
    public string? DamageSummary { get; init; }

    [StringLength(4000)]
    [JsonPropertyName("damage_description")]
    public string? DamageDescription { get; init; }
}
