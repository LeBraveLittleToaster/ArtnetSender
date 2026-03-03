using Newtonsoft.Json;
using NodaTime;

namespace LumenForgeServer.Auth.Domain;

/// <summary>
/// Represents an authenticated user in the system.
/// </summary>
public class KcUserReference
{
    /// <summary>
    /// Internal database identifier.
    /// </summary>
    public long Id { get; set; }

    public required string UsernameMirror { get; set; }
    public required string EmailMirror { get; set; }
    public required string FirstNameMirror { get; set; }
    public required string LastNameMirror { get; set; }

    /// <summary>
    /// Timestamp when the user joined the system.
    /// </summary>
    /// [JsonProperty("id"joinedAt
    public required Instant JoinedAt { get; set; }

    /// <summary>
    /// Keycloak subject identifier ("sub") for the user.
    /// </summary>
    [JsonProperty("userKcId")]
    public required string UserKcId { get; set; }

    /// <summary>
    /// Group memberships for the user.
    /// </summary>
    public List<GroupUser> GroupUsers { get; } = [];
}