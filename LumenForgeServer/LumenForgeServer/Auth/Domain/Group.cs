using NodaTime;

namespace LumenForgeServer.Auth.Domain;

/// <summary>
/// Represents a group of users and its assigned roles.
/// </summary>
public class Group
{
    /// <summary>
    /// Internal database identifier.
    /// </summary>
    public long Id { get; set; }
    /// <summary>
    /// External identifier used for API interactions.
    /// </summary>
    public Guid Guid { get; set; }

    /// <summary>
    /// Human-readable name for the group.
    /// </summary>
    public required string Name { get; set; } = null!;
    /// <summary>
    /// Short description explaining the group's purpose.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Timestamp when the group was created.
    /// </summary>
    public required Instant CreatedAt { get; set; }
    /// <summary>
    /// Timestamp when the group was last updated.
    /// </summary>
    public required Instant UpdatedAt { get; set; }

    /// <summary>
    /// Users that belong to the group.
    /// </summary>
    public List<GroupUser> GroupUsers { get; } = [];
    /// <summary>
    /// Roles assigned to the group.
    /// </summary>
    public List<GroupPermissions> GroupRoles { get; } = [];
}
