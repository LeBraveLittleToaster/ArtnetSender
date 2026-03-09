using System.Text.Json.Serialization;
using LumenForgeServer.Auth.Domain;
using LumenForgeServer.Common.Database;
using NodaTime;

namespace LumenForgeServer.Auth.Dto.Views;

/// <summary>
/// Subset of the User class for API response
/// </summary>
public record UserView
{
    /// <summary>
    /// Timestamp when the user joined the system.
    /// </summary>
    [JsonPropertyName("joined_at")]
    public required Instant JoinedAt { get; set; } 
    /// <summary>
    /// Keycloak subject identifier ("sub") for the user.
    /// </summary>
    [JsonPropertyName("user_kc_id")]
    public required string UserKcId { get; set; }
    /// <summary>
    /// Group memberships for the user.
    /// </summary>
    [JsonPropertyName("groups")]
    public List<GroupView> Groups { get; private set; } = [];
    
    [JsonPropertyName("username")] public required string Username{ get; set; }
    [JsonPropertyName("email")] public required string Email{ get; set; }
    [JsonPropertyName("firstName")] public required string FirstName{ get; set; }
    [JsonPropertyName("lastName")] public required string LastName{ get; set; }

    public static UserView FromEntity(KcUserReference tEntity)
    {
        return new UserView
        {
            UserKcId = tEntity.UserKcId,
            JoinedAt = tEntity.JoinedAt,
            Groups = [],
            Username = tEntity.UsernameMirror,
            Email = tEntity.EmailMirror,
            FirstName = tEntity.FirstNameMirror,
            LastName = tEntity.LastNameMirror,
        };
    }
    public static UserView FromEntityWithGroups(KcUserReference tEntity, List<GroupView> tGroups)
    {
        return new UserView
        {
            UserKcId = tEntity.UserKcId,
            JoinedAt = tEntity.JoinedAt,
            Groups = tGroups,
            Username = tEntity.UsernameMirror,
            Email = tEntity.EmailMirror,
            FirstName = tEntity.FirstNameMirror,
            LastName = tEntity.LastNameMirror,
        };
    }
}