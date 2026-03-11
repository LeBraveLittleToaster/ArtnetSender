using LumenForgeServer.Rentals.Domain;
using NodaTime;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Rentals.Dto.View;

/// <summary>
/// Read model for a rental status lookup entry.
/// </summary>
public sealed record RentalStatusView
{
    [JsonPropertyName("uuid")]
    public Guid Uuid { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("created_at")]
    public Instant CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public Instant UpdatedAt { get; init; }

    public static RentalStatusView FromEntity(RentalStatus e) => new()
    {
        Uuid = e.Uuid,
        Name = e.Name,
        Description = e.Description,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
    };
}
