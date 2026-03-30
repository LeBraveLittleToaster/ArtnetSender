using System.Security.Claims;

namespace LumenForgeServer.Auth.Domain;

public static class RoleClaims
{
    /// <summary>
    /// Complete list of application permission role names.
    /// </summary>
    /// <remarks>
    /// Core concept: this array is used when validating or projecting app role claims against the
    /// <see cref="Permissions"/> enum.
    /// Potential side effects: read-only static initialization only.
    /// </remarks>
    public static readonly string[] AllAppRoles =
        Enum.GetValues<Permissions>()
            .Select(r => r.ToString())
            .ToArray();

    /// <summary>
    /// Adds application permission claims from enum values to an identity.
    /// Core concept: normalizes role values to distinct claim entries.
    /// </summary>
    /// <remarks>Potential side effects: mutates the supplied <see cref="ClaimsIdentity"/> by appending role claims.</remarks>
    /// <param name="identity">Identity to mutate with role claims.</param>
    /// <param name="roles">Application permissions that should be emitted as role claims.</param>
    public static void AddAppRoles(this ClaimsIdentity identity, IEnumerable<Permissions> roles)
    {
        foreach (var role in roles.Distinct())
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, role.ToString()));
        }
    }

    /// <summary>
    /// Adds application permission claims from role-name strings to an identity.
    /// Core concept: filters empty names and de-duplicates entries before claim creation.
    /// </summary>
    /// <remarks>Potential side effects: mutates the supplied <see cref="ClaimsIdentity"/> by appending role claims.</remarks>
    /// <param name="identity">Identity to mutate with role claims.</param>
    /// <param name="roleNames">Role names to append as <see cref="ClaimTypes.Role"/> claims.</param>
    public static void AddAppRoles(this ClaimsIdentity identity, IEnumerable<string> roleNames)
    {
        foreach (var name in roleNames.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct())
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, name));
        }
    }

    /// <summary>
    /// Checks whether the caller carries the Keycloak realm-admin role.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="principal">Principal whose role claims are evaluated.</param>
    /// <returns><see langword="true"/> when the caller is in role <c>REALM_ADMIN</c>; otherwise <see langword="false"/>.</returns>
    public static bool HasRealmAdmin(this ClaimsPrincipal principal) =>
        principal.IsInRole("REALM_ADMIN");

    /// <summary>
    /// Extracts application permissions from role claims.
    /// Core concept: parses role claim values into <see cref="Permissions"/> values and removes duplicates.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="principal">Principal whose role claims are read.</param>
    /// <returns>Distinct application permissions represented by the caller's role claims.</returns>
    public static IReadOnlyList<Permissions> GetAppPermissions(this ClaimsPrincipal principal)
    {
        return principal.FindAll(ClaimTypes.Role)
            .Select(c => Enum.TryParse<Permissions>(c.Value, out var permission)
                ? (Permissions?)permission
                : null)
            .Where(p => p.HasValue)
            .Select(p => p!.Value)
            .Distinct()
            .ToList();
    }
}
