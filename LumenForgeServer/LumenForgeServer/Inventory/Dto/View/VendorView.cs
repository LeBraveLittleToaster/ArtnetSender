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

    /// <summary>
    /// Executes the from entity operation.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="vendor">Input value used by this operation.</param>
    /// <returns>The operation result.</returns>
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
