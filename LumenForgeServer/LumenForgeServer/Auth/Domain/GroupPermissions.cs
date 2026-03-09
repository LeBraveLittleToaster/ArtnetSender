namespace LumenForgeServer.Auth.Domain;

/// <summary>
/// Join entity linking a group to a role.
/// </summary>
public class GroupPermissions
{
    /// <summary>
    /// Internal group identifier.
    /// </summary>
    public long GroupId { get; set; }
    /// <summary>
    /// Role assigned to the group.
    /// </summary>
    public Permissions PermissionsId { get; set; }

    /// <summary>
    /// Navigation to the group.
    /// </summary>
    public Group Group { get; set; } = null!;
    
}
