using LumenForgeServer.Common;
using LumenForgeServer.Rentals.Domain;
using NodaTime;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Rentals.Dto.View;

/// <summary>
/// View model for the core rental data aggregate.
/// </summary>
public sealed record RentalView
{
    [JsonPropertyName("uuid")]
    public Guid Uuid { get; init; }

    [JsonPropertyName("customer_kc_id")]
    public required string CustomerKcId { get; init; }

    [JsonPropertyName("customer_name")]
    public string? CustomerName { get; init; }

    [JsonPropertyName("customer_email")]
    public string? CustomerEmail { get; init; }

    [JsonPropertyName("purpose")]
    public string? Purpose { get; init; }

    [JsonPropertyName("requested_start")]
    public Instant RequestedStart { get; init; }

    [JsonPropertyName("requested_end")]
    public Instant RequestedEnd { get; init; }

    [JsonPropertyName("priority")]
    public RentalPriority Priority { get; init; }

    [JsonPropertyName("notes")]
    public string? Notes { get; init; }

    [JsonPropertyName("created_at")]
    public Instant CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public Instant UpdatedAt { get; init; }

    [JsonPropertyName("answers")]
    public IReadOnlyList<AnswerView> Answers { get; init; } = [];

    public static RentalView FromEntity(Rental rental) => new()
    {
        Uuid = rental.Uuid,
        CustomerKcId = rental.CustomerKcId,
        CustomerName = rental.CustomerName,
        CustomerEmail = rental.CustomerEmail,
        Purpose = rental.Purpose,
        RequestedStart = rental.RequestedStart,
        RequestedEnd = rental.RequestedEnd,
        Priority = rental.Priority,
        Notes = rental.Notes,
        CreatedAt = rental.CreatedAt,
        UpdatedAt = rental.UpdatedAt,
        Answers = rental.Answers.Select(AnswerView.FromEntity).ToList()
    };
}
