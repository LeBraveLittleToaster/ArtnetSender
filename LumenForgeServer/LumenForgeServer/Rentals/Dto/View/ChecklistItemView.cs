using LumenForgeServer.Rentals.Domain;
using NodaTime;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Rentals.Dto.View;

/// <summary>
/// Read model for a single checklist inspection row.
/// <see cref="IsChecked"/> is <c>false</c> for rows that have not yet been submitted by staff.
/// </summary>
public sealed record ChecklistItemView
{
    [JsonPropertyName("uuid")]
    public Guid Uuid { get; init; }

    [JsonPropertyName("rental_item_uuid")]
    public Guid RentalItemUuid { get; init; }

    [JsonPropertyName("is_checked")]
    public bool IsChecked { get; init; }

    [JsonPropertyName("quantity_checked")]
    public decimal QuantityChecked { get; init; }

    [JsonPropertyName("condition_ok")]
    public bool ConditionOk { get; init; }

    [JsonPropertyName("condition_notes")]
    public string? ConditionNotes { get; init; }

    [JsonPropertyName("damaged_quantity")]
    public decimal DamagedQuantity { get; init; }

    [JsonPropertyName("damage_summary")]
    public string? DamageSummary { get; init; }

    [JsonPropertyName("damage_description")]
    public string? DamageDescription { get; init; }

    [JsonPropertyName("created_at")]
    public Instant CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public Instant UpdatedAt { get; init; }

    public static ChecklistItemView FromEntity(ChecklistItem e) => new()
    {
        Uuid = e.Uuid,
        RentalItemUuid = e.RentalItem.Uuid,
        IsChecked = e.IsChecked,
        QuantityChecked = e.QuantityChecked,
        ConditionOk = e.ConditionOk,
        ConditionNotes = e.ConditionNotes,
        DamagedQuantity = e.DamagedQuantity,
        DamageSummary = e.DamageSummary,
        DamageDescription = e.DamageDescription,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
    };
}
