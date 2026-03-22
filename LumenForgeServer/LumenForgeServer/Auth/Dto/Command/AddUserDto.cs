using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LumenForgeServer.Auth.Dto.Command;

/// <summary>
/// Payload for creating a user record from a Keycloak subject identifier.
/// </summary>
public record AddUserDto
{
    /// <summary>
    /// Keycloak subject identifier ("sub") for the user to create.
    /// </summary>
    [Required]
    [MinLength(1)]
    [RegularExpression(@".*\S.*")]
    [JsonPropertyName("userKcId")]
    public required string userKcId { get; init; }
}

/// <summary>
/// Payload for creating a new user in Keycloak and the local database.
/// </summary>
public record AddKcUserDto
{
    /// <summary>Login username for the Keycloak account.</summary>
    [Required]
    [MinLength(1)]
    [RegularExpression(@".*\S.*")]
    public required string Username { get; init; }

    /// <summary>Initial password for the Keycloak account.</summary>
    [Required]
    [MinLength(1)]
    [RegularExpression(@".*\S.*")]
    public required string Password { get; init; }

    /// <summary>E-mail address for the new user.</summary>
    [Required]
    [MinLength(1)]
    [RegularExpression(@".*\S.*")]
    public required string Email { get; init; }

    /// <summary>First name of the user.</summary>
    [Required]
    [MinLength(1)]
    [RegularExpression(@".*\S.*")]
    public required string FirstName { get; init; }

    /// <summary>Last name of the user.</summary>
    [Required]
    [MinLength(1)]
    [RegularExpression(@".*\S.*")]
    public required string LastName { get; init; }

    /// <summary>Optional Keycloak groups to assign to the user on creation.</summary>
    public string[] Groups { get; init; } = [];
    /// <summary>Optional Keycloak realm roles to assign to the user on creation.</summary>
    public string[] RealmRoles { get; init; } = [];

}

