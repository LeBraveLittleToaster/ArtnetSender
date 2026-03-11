using LumenForgeServer.Common;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Rentals.Dto.Command;

/// <summary>
/// Payload for creating a new rental.
/// </summary>
public sealed record CreateRentalDto
{
    [Required]
    [JsonPropertyName("rental_status_guid")]
    public required Guid RentalStatusGuid { get; init; }

    [StringLength(512)]
    [JsonPropertyName("request_title")]
    public string? RequestTitle { get; init; }

    [StringLength(4000)]
    [JsonPropertyName("request_description")]
    public string? RequestDescription { get; init; }

    [StringLength(512)]
    [JsonPropertyName("event_name")]
    public string? EventName { get; init; }

    [StringLength(4000)]
    [JsonPropertyName("customer_notes")]
    public string? CustomerNotes { get; init; }

    [StringLength(1000)]
    [JsonPropertyName("delivery_address")]
    public string? DeliveryAddress { get; init; }

    [JsonPropertyName("priority")]
    public RentalPriority Priority { get; init; } = RentalPriority.NORMAL;

    /// <summary>ISO-8601 instant string.</summary>
    [JsonPropertyName("planned_pickup_at")]
    public string? PlannedPickupAt { get; init; }

    /// <summary>ISO-8601 instant string.</summary>
    [JsonPropertyName("planned_return_at")]
    public string? PlannedReturnAt { get; init; }
}
