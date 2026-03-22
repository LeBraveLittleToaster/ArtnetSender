using LumenForgeServer.Common;
using LumenForgeServer.Rentals.Domain;
using NodaTime;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Rentals.Dto.View;

/// <summary>
/// View model for a checklist including its line items.
/// </summary>
public sealed record ChecklistView
{
    [JsonPropertyName("guid")]
    public Guid Guid { get; init; }

    [JsonPropertyName("checklist_type")]
    public ChecklistType ChecklistType { get; init; }

    [JsonPropertyName("is_signed")]
    public bool IsSigned { get; init; }

    [JsonPropertyName("signed_by_kc_id")]
    public string? SignedByKcId { get; init; }

    [JsonPropertyName("signed_at")]
    public Instant? SignedAt { get; init; }

    [JsonPropertyName("created_at")]
    public Instant CreatedAt { get; init; }

    [JsonPropertyName("items")]
    public IReadOnlyList<ChecklistItemView> Items { get; init; } = [];

    public static ChecklistView FromEntity(Checklist checklist) => new()
    {
        Guid = checklist.Guid,
        ChecklistType = checklist.ChecklistType,
        IsSigned = checklist.IsSigned,
        SignedByKcId = checklist.SignedByKcId,
        SignedAt = checklist.SignedAt,
        CreatedAt = checklist.CreatedAt,
        Items = checklist.Items.Select(ChecklistItemView.FromEntity).ToList()
    };
}

/// <summary>
/// View model for a single checklist line item.
/// </summary>
public sealed record ChecklistItemView
{
    [JsonPropertyName("guid")]
    public Guid Guid { get; init; }

    [JsonPropertyName("stock_binding_guid")]
    public Guid StockBindingGuid { get; init; }

    [JsonPropertyName("device_name")]
    public string DeviceName { get; init; } = null!;

    [JsonPropertyName("is_scanned")]
    public bool IsScanned { get; init; }

    [JsonPropertyName("scanned_value")]
    public string? ScannedValue { get; init; }

    [JsonPropertyName("scanned_by_kc_id")]
    public string? ScannedByKcId { get; init; }

    [JsonPropertyName("scanned_at")]
    public Instant? ScannedAt { get; init; }

    public static ChecklistItemView FromEntity(ChecklistItem item) => new()
    {
        Guid = item.Guid,
        StockBindingGuid = item.StockBindingGuid,
        DeviceName = item.DeviceName,
        IsScanned = item.IsScanned,
        ScannedValue = item.ScannedValue,
        ScannedByKcId = item.ScannedByKcId,
        ScannedAt = item.ScannedAt
    };
}
