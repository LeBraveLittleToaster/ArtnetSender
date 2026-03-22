using LumenForgeServer.Inventory.Domain;
using NodaTime;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Inventory.Dto.View;

/// <summary>
/// View model for vendors.
/// </summary>
public sealed record VendorView
{
    /// <summary>Unique vendor identifier.</summary>
    [JsonPropertyName("guid")]
    public Guid Guid { get; init; }

    /// <summary>Vendor display name.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>Timestamp when the vendor was created.</summary>
    [JsonPropertyName("created_at")]
    public Instant CreatedAt { get; init; }

    /// <summary>Timestamp when the vendor was last updated.</summary>
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
