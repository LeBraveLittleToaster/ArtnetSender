using LumenForgeServer.Auth.Domain;
using NodaTime;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Auth.Dto.Views;

/// <summary>
/// Read model representing a group for API responses.
/// </summary>
public class GroupView
{
    /// <summary>
    /// External identifier used for API interactions.
    /// </summary>
    [JsonPropertyName("guid")]
    public Guid Guid { get; set; }

    /// <summary>
    /// Human-readable name for the group.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; } = null!;
    /// <summary>
    /// Short description explaining the group's purpose.
    /// </summary>
    [JsonPropertyName("description")]
    public required string Description { get; set; }

    /// <summary>
    /// Timestamp when the group was created.
    /// </summary>
    [JsonPropertyName("created_at")]
    public required Instant CreatedAt { get; set; }
    /// <summary>
    /// Timestamp when the group was last updated.
    /// </summary>
    [JsonPropertyName("updated_at")]
    public required Instant UpdatedAt { get; set; }

    /// <summary>
    /// Permissions the group is assigned to
    /// </summary>
    [JsonPropertyName("permissions")]
    public required List<Permissions> Permissions { get; set; }

    public static GroupView FromEntity(Group tEntity)
    {
        var permissions = (tEntity.GroupRoles.Count == 0) ?
            []
            : tEntity.GroupRoles.Select(gr => gr.Permission).ToList();
        return new GroupView
        {
            Guid = tEntity.Guid,
            Name = tEntity.Name,
            Description = tEntity.Description,
            CreatedAt = tEntity.CreatedAt,
            UpdatedAt = tEntity.UpdatedAt,
            Permissions = permissions,
        };
    }
}
