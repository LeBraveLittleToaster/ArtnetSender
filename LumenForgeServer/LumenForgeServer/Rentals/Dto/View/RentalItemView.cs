using LumenForgeServer.Common;
using LumenForgeServer.Inventory.Dto.View;
using LumenForgeServer.Rentals.Domain;
using NodaTime;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Rentals.Dto.View;

/// <summary>
/// Read model for a rental line item including its allocated stock bindings.
/// </summary>
public sealed record RentalItemView
{
    [JsonPropertyName("uuid")]
    public Guid Uuid { get; init; }

    [JsonPropertyName("status")]
    public RentalItemStatus Status { get; init; }

    [JsonPropertyName("quantity_requested")]
    public decimal QuantityRequested { get; init; }

    [JsonPropertyName("quantity_approved")]
    public decimal? QuantityApproved { get; init; }

    [JsonPropertyName("quantity_picked_up")]
    public decimal? QuantityPickedUp { get; init; }

    [JsonPropertyName("quantity_returned")]
    public decimal? QuantityReturned { get; init; }

    [JsonPropertyName("quantity_damaged")]
    public decimal? QuantityDamaged { get; init; }

    [JsonPropertyName("quantity_lost")]
    public decimal? QuantityLost { get; init; }

    [JsonPropertyName("is_approved")]
    public bool IsApproved { get; init; }

    [JsonPropertyName("approved_at")]
    public Instant? ApprovedAt { get; init; }

    [JsonPropertyName("approved_by_user_id")]
    public string? ApprovedByUserId { get; init; }

    [JsonPropertyName("rejection_reason")]
    public string? RejectionReason { get; init; }

    [JsonPropertyName("planned_pickup_at")]
    public Instant? PlannedPickupAt { get; init; }

    [JsonPropertyName("planned_return_at")]
    public Instant? PlannedReturnAt { get; init; }

    [JsonPropertyName("actual_pickup_at")]
    public Instant? ActualPickupAt { get; init; }

    [JsonPropertyName("actual_return_at")]
    public Instant? ActualReturnAt { get; init; }

    [JsonPropertyName("daily_rate")]
    public decimal? DailyRate { get; init; }

    [JsonPropertyName("deposit_amount")]
    public decimal? DepositAmount { get; init; }

    [JsonPropertyName("condition_notes")]
    public string? ConditionNotes { get; init; }

    [JsonPropertyName("pickup_notes")]
    public string? PickupNotes { get; init; }

    [JsonPropertyName("return_notes")]
    public string? ReturnNotes { get; init; }

    [JsonPropertyName("stock_bindings")]
    public IReadOnlyList<StockBindingView> StockBindings { get; init; } = [];

    [JsonPropertyName("created_at")]
    public Instant CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public Instant UpdatedAt { get; init; }

    public static RentalItemView FromEntity(RentalItem e) => new()
    {
        Uuid = e.Uuid,
        Status = e.Status,
        QuantityRequested = e.QuantityRequested,
        QuantityApproved = e.QuantityApproved,
        QuantityPickedUp = e.QuantityPickedUp,
        QuantityReturned = e.QuantityReturned,
        QuantityDamaged = e.QuantityDamaged,
        QuantityLost = e.QuantityLost,
        IsApproved = e.IsApproved,
        ApprovedAt = e.ApprovedAt,
        ApprovedByUserId = e.ApprovedByUserId,
        RejectionReason = e.RejectionReason,
        PlannedPickupAt = e.PlannedPickupAt,
        PlannedReturnAt = e.PlannedReturnAt,
        ActualPickupAt = e.ActualPickupAt,
        ActualReturnAt = e.ActualReturnAt,
        DailyRate = e.DailyRate,
        DepositAmount = e.DepositAmount,
        ConditionNotes = e.ConditionNotes,
        PickupNotes = e.PickupNotes,
        ReturnNotes = e.ReturnNotes,
        StockBindings = e.StockBindings
            .Select(StockBindingView.FromEntity)
            .OrderBy(sb => sb.Start)
            .ToList(),
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
    };
}
