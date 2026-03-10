using NodaTime;

namespace LumenForgeServer.Auth.Domain;

/// <summary>
/// Join entity linking a user to a group membership.
/// </summary>
public class GroupUser
{
    /// <summary>
    /// Internal group identifier.
    /// </summary>
    public long GroupId { get; set; }
    /// <summary>
    /// Internal user identifier.
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// Navigation to the group.
    /// </summary>
    public Group Group { get; set; } = null!;
    /// <summary>
    /// Navigation to the user.
    /// </summary>
    public KcUserReference KcUserReference { get; set; } = null!;

    /// <summary>
    /// Timestamp when the user joined the group.
    /// </summary>
    public Instant JoinedAt { get; set; }
    /// <summary>
    /// Optional Keycloak subject identifier of the actor who made the assignment.
    /// </summary>
    public string? AssignedByKeycloakId { get; set; }
}
