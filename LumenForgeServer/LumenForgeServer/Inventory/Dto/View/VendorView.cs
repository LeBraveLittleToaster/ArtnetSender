using LumenForgeServer.Inventory.Domain;
using NodaTime;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Inventory.Dto.View;

/// <summary>
/// View model for vendors.
/// </summary>
public sealed record VendorView
{
    [JsonPropertyName("Guid")]
    public Guid Guid { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("created_at")]
    public Instant CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public Instant UpdatedAt { get; init; }

    public static VendorView FromEntity(Vendor vendor)
    {
        return new VendorView
        {
            Guid = vendor.Guid,
            Name = vendor.Name,
            CreatedAt = vendor.CreatedAt,
            UpdatedAt = vendor.UpdatedAt
        };
    }
}
