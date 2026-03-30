using LumenForgeServer.Inventory.Domain;
using NodaTime;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Inventory.Dto.View;

/// <summary>
/// View model for inventory categories.
/// </summary>
public sealed record CategoryView
{
    /// <summary>Unique category identifier.</summary>
    [JsonPropertyName("guid")]
    public Guid Guid { get; init; }

    /// <summary>Category display name.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>Optional category description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>Timestamp when the category was created.</summary>
    [JsonPropertyName("created_at")]
    public Instant CreatedAt { get; init; }

    /// <summary>Timestamp when the category was last updated.</summary>
    [JsonPropertyName("updated_at")]
    public Instant UpdatedAt { get; init; }

    /// <summary>
    /// Executes the from entity operation.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="category">Input value used by this operation.</param>
    /// <returns>The operation result.</returns>
    public static CategoryView FromEntity(Category category)
    {
        return new CategoryView
        {
            Guid = category.Guid,
            Name = category.Name,
            Description = category.Description,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };
    }
}
