using LumenForgeServer.Auth.Domain;
using NodaTime;
using System.Text.Json.Serialization;

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

    /// <summary>Keycloak login username.</summary>
    [JsonPropertyName("username")] public required string Username { get; set; }
    /// <summary>User’s e-mail address.</summary>
    [JsonPropertyName("email")] public required string Email { get; set; }
    /// <summary>User’s first name.</summary>
    [JsonPropertyName("firstName")] public required string FirstName { get; set; }
    /// <summary>User’s last name.</summary>
    [JsonPropertyName("lastName")] public required string LastName { get; set; }

    [JsonPropertyName("effective_permissions")]
    public List<Permissions> EffectivePermissions { get; private set; } = [];

    [JsonPropertyName("rental_scopes")]
    public RentalScopesView RentalScopes { get; private set; } = RentalScopesView.None;

    /// <summary>
    /// Executes the from entity operation.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="tEntity">Input value used by this operation.</param>
    /// <param name="effectivePermissions">Input value used by this operation.</param>
    /// <returns>The operation result.</returns>
    public static UserView FromEntity(
        KcUserReference tEntity,
        IReadOnlyCollection<Permissions>? effectivePermissions = null)
    {
        var permissions = (effectivePermissions ?? []).Distinct().OrderBy(p => p).ToList();
        return new UserView
        {
            UserKcId = tEntity.UserKcId,
            JoinedAt = tEntity.JoinedAt,
            Groups = [],
            Username = tEntity.UsernameMirror,
            Email = tEntity.EmailMirror,
            FirstName = tEntity.FirstNameMirror,
            LastName = tEntity.LastNameMirror,
            EffectivePermissions = permissions,
            RentalScopes = RentalScopesView.FromPermissions(permissions)
        };
    }

    /// <summary>
    /// Executes the from entity with groups operation.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="tEntity">Input value used by this operation.</param>
    /// <param name="tGroups">Input value used by this operation.</param>
    /// <param name="effectivePermissions">Input value used by this operation.</param>
    /// <returns>The operation result.</returns>
    public static UserView FromEntityWithGroups(
        KcUserReference tEntity,
        List<GroupView> tGroups,
        IReadOnlyCollection<Permissions>? effectivePermissions = null)
    {
        var permissions = (effectivePermissions ?? []).Distinct().OrderBy(p => p).ToList();
        return new UserView
        {
            UserKcId = tEntity.UserKcId,
            JoinedAt = tEntity.JoinedAt,
            Groups = tGroups,
            Username = tEntity.UsernameMirror,
            Email = tEntity.EmailMirror,
            FirstName = tEntity.FirstNameMirror,
            LastName = tEntity.LastNameMirror,
            EffectivePermissions = permissions,
            RentalScopes = RentalScopesView.FromPermissions(permissions)
        };
    }
}

public enum ScopeLevel
{
    None,
    Own,
    Group,
    OwnAndGroup,
    All
}

public sealed record RentalScopesView
{
    public static RentalScopesView None { get; } = new();

    [JsonPropertyName("read")]
    public ScopeLevel Read { get; init; } = ScopeLevel.None;

    [JsonPropertyName("create")]
    public ScopeLevel Create { get; init; } = ScopeLevel.None;

    [JsonPropertyName("update")]
    public ScopeLevel Update { get; init; } = ScopeLevel.None;

    [JsonPropertyName("delete")]
    public ScopeLevel Delete { get; init; } = ScopeLevel.None;

    /// <summary>
    /// Executes the from permissions operation.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="permissions">Input value used by this operation.</param>
    /// <returns>The operation result.</returns>
    public static RentalScopesView FromPermissions(IReadOnlyCollection<Permissions> permissions)
    {
        return new RentalScopesView
        {
            Read = BuildScopedLevel(permissions, Permissions.RentalReadAll),
            Create = BuildScopedLevel(permissions, Permissions.RentalCreate),
            Update = BuildScopedLevel(permissions, Permissions.RentalUpdateAll),
            Delete = permissions.Contains(Permissions.RentalDeleteAll)
                ? ScopeLevel.All
                : ScopeLevel.None
        };
    }

    /// <summary>
    /// Executes the build scoped level operation.
    /// </summary>
    /// <remarks>Potential side effects: may modify state as part of this operation.</remarks>
    /// <param name="permissions">Input value used by this operation.</param>
    /// <param name="globalPermission">Input value used by this operation.</param>
    /// <returns>The operation result.</returns>
    private static ScopeLevel BuildScopedLevel(IReadOnlyCollection<Permissions> permissions, Permissions globalPermission)
    {
        if (permissions.Contains(globalPermission))
            return ScopeLevel.All;

        var hasOwn = permissions.Contains(Permissions.RentalUserOwn);
        var hasGroup = permissions.Contains(Permissions.RentalGroup);

        return (hasOwn, hasGroup) switch
        {
            (true, true) => ScopeLevel.OwnAndGroup,
            (true, false) => ScopeLevel.Own,
            (false, true) => ScopeLevel.Group,
            _ => ScopeLevel.None
        };
    }
}
