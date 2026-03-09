using LumenForgeServer.Auth.Domain;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Auth.Dto.Command;

/// <summary>
/// Payload for creating a group with a name and description.
/// </summary>
public record AddGroupDto
{
    /// <summary>
    /// Human-readable name for the group.
    /// </summary>
    [Required]
    [MinLength(1)]
    [RegularExpression(@".*\S.*")]
    [JsonPropertyName("name")]
    public required string Name { get; set; } = null!;
    /// <summary>
    /// Short description explaining the group's purpose.
    /// </summary>
    [Required]
    [MinLength(10)]
    [RegularExpression(@".*\S.*")]
    [JsonPropertyName("description")]
    public required string Description { get; set; }

    /// <summary>
    /// Roles assigned to that group
    /// </summary>
    [JsonPropertyName("roles")]
    public required Permissions[] Roles { get; set; }
}
