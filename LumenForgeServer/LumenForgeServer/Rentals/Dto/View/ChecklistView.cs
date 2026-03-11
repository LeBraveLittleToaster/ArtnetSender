using LumenForgeServer.Common;
using LumenForgeServer.Rentals.Domain;
using NodaTime;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Rentals.Dto.View;

/// <summary>
/// Read model for a rental checklist including its inspection items and completion progress.
/// </summary>
public sealed record ChecklistView
{
    [JsonPropertyName("uuid")]
    public Guid Uuid { get; init; }

    [JsonPropertyName("checklist_type")]
    public ChecklistType ChecklistType { get; init; }

    /// <summary>UUID of the PICKUP checklist this DROPOFF checklist was derived from, if any.</summary>
    [JsonPropertyName("source_checklist_uuid")]
    public Guid? SourceChecklistUuid { get; init; }

    [JsonPropertyName("generated_by_user_id")]
    public string? GeneratedByUserId { get; init; }

    [JsonPropertyName("generated_at")]
    public Instant GeneratedAt { get; init; }

    [JsonPropertyName("signed_at")]
    public Instant? SignedAt { get; init; }

    [JsonPropertyName("signed_by_user_id")]
    public string? SignedByUserId { get; init; }

    [JsonPropertyName("notes")]
    public string? Notes { get; init; }

    /// <summary>Total number of line items on this checklist.</summary>
    [JsonPropertyName("total_items")]
    public int TotalItems { get; init; }

    /// <summary>Number of items whose <c>is_checked</c> flag is <c>true</c>.</summary>
    [JsonPropertyName("checked_items_count")]
    public int CheckedItemsCount { get; init; }

    /// <summary><c>true</c> when every item has been inspected (all <c>is_checked == true</c>).</summary>
    [JsonPropertyName("is_complete")]
    public bool IsComplete { get; init; }

    /// <summary><c>true</c> once the checklist has been signed and is immutable.</summary>
    [JsonPropertyName("is_signed")]
    public bool IsSigned { get; init; }

    [JsonPropertyName("created_at")]
    public Instant CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public Instant UpdatedAt { get; init; }

    [JsonPropertyName("items")]
    public IReadOnlyList<ChecklistItemView> Items { get; init; } = [];

    public static ChecklistView FromEntity(Checklist e) => new()
    {
        Uuid = e.Uuid,
        ChecklistType = e.ChecklistType,
        SourceChecklistUuid = e.SourceChecklist?.Uuid,
        GeneratedByUserId = e.GeneratedByUserId,
        GeneratedAt = e.GeneratedAt,
        SignedAt = e.SignedAt,
        SignedByUserId = e.SignedByUserId,
        Notes = e.Notes,
        TotalItems = e.Items.Count,
        CheckedItemsCount = e.Items.Count(i => i.IsChecked),
        IsComplete = e.Items.Count > 0 && e.Items.All(i => i.IsChecked),
        IsSigned = e.SignedAt.HasValue,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
        Items = e.Items
            .Select(ChecklistItemView.FromEntity)
            .OrderBy(i => i.RentalItemUuid)
            .ToList(),
    };
}
