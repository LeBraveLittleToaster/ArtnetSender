using LumenForgeServer.Auth.Domain;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Auth.Dto.Command;

/// <summary>
/// Payload for assigning a user to a group.
/// </summary>
public record AssignUserToGroupDto
{
    /// <summary>
    /// Optional Keycloak subject identifier for the actor performing the assignment.
    /// </summary>
    [JsonPropertyName("assigneeKcId")]
    public string? assigneeKcId { get; init; }
    /// <summary>
    /// Keycloak subject identifier for the user being assigned.
    /// </summary>
    [Required]
    [MinLength(1)]
    [RegularExpression(@".*\S.*")]
    [JsonPropertyName("userKcId")]
    public required string userKcId { get; init; }
}

/// <summary>
/// Payload for replacing all role assignments on a group.
/// </summary>
public record AssignGroupRolesDto
{
    /// <summary>Complete set of roles to assign. Existing roles are replaced.</summary>
    [JsonPropertyName("roles")]
    public required Permissions[] Roles { get; init; }
}