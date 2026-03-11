using LumenForgeServer.Common;
using LumenForgeServer.Rentals.Domain;
using NodaTime;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Rentals.Dto.View;

/// <summary>
/// Read model for a rental. Items are populated only when the <c>Items</c> include flag is set.
/// </summary>
public sealed record RentalView
{
    [JsonPropertyName("uuid")]
    public Guid Uuid { get; init; }

    [JsonPropertyName("rental_status_uuid")]
    public Guid RentalStatusUuid { get; init; }

    [JsonPropertyName("rental_status_name")]
    public required string RentalStatusName { get; init; }

    [JsonPropertyName("customer_user_id")]
    public required string CustomerUserId { get; init; }

    [JsonPropertyName("request_title")]
    public string? RequestTitle { get; init; }

    [JsonPropertyName("request_description")]
    public string? RequestDescription { get; init; }

    [JsonPropertyName("event_name")]
    public string? EventName { get; init; }

    [JsonPropertyName("customer_notes")]
    public string? CustomerNotes { get; init; }

    [JsonPropertyName("delivery_address")]
    public string? DeliveryAddress { get; init; }

    [JsonPropertyName("priority")]
    public RentalPriority Priority { get; init; }

    [JsonPropertyName("requested_at")]
    public Instant? RequestedAt { get; init; }

    [JsonPropertyName("planned_pickup_at")]
    public Instant? PlannedPickupAt { get; init; }

    [JsonPropertyName("planned_return_at")]
    public Instant? PlannedReturnAt { get; init; }

    [JsonPropertyName("created_at")]
    public Instant CreatedAt { get; init; }

    [JsonPropertyName("pickup_at")]
    public Instant? PickupAt { get; init; }

    [JsonPropertyName("dropoff_at")]
    public Instant? DropoffAt { get; init; }

    [JsonPropertyName("completed_at")]
    public Instant? CompletedAt { get; init; }

    [JsonPropertyName("invoiced_at")]
    public Instant? InvoicedAt { get; init; }

    [JsonPropertyName("paid_at")]
    public Instant? PaidAt { get; init; }

    [JsonPropertyName("reported_at")]
    public Instant? ReportedAt { get; init; }

    [JsonPropertyName("assigned_by_user_id")]
    public string? AssignedByUserId { get; init; }

    [JsonPropertyName("assigned_at")]
    public Instant? AssignedAt { get; init; }

    [JsonPropertyName("is_scrapped")]
    public bool IsScrapped { get; init; }

    [JsonPropertyName("scrapped_at")]
    public Instant? ScrappedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public Instant UpdatedAt { get; init; }

    [JsonPropertyName("items")]
    public IReadOnlyList<RentalItemView> Items { get; init; } = [];

    public static RentalView FromEntity(Rental e) => new()
    {
        Uuid = e.Uuid,
        RentalStatusUuid = e.RentalStatus.Uuid,
        RentalStatusName = e.RentalStatus.Name,
        CustomerUserId = e.CustomerUserId,
        RequestTitle = e.RequestTitle,
        RequestDescription = e.RequestDescription,
        EventName = e.EventName,
        CustomerNotes = e.CustomerNotes,
        DeliveryAddress = e.DeliveryAddress,
        Priority = e.Priority,
        RequestedAt = e.RequestedAt,
        PlannedPickupAt = e.PlannedPickupAt,
        PlannedReturnAt = e.PlannedReturnAt,
        CreatedAt = e.CreatedAt,
        PickupAt = e.PickupAt,
        DropoffAt = e.DropoffAt,
        CompletedAt = e.CompletedAt,
        InvoicedAt = e.InvoicedAt,
        PaidAt = e.PaidAt,
        ReportedAt = e.ReportedAt,
        AssignedByUserId = e.AssignedByUserId,
        AssignedAt = e.AssignedAt,
        IsScrapped = e.IsScrapped,
        ScrappedAt = e.ScrappedAt,
        UpdatedAt = e.UpdatedAt,
        Items = e.Items
            .Select(RentalItemView.FromEntity)
            .OrderBy(i => i.CreatedAt)
            .ToList(),
    };
}
