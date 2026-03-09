using System.Security.Claims;

namespace LumenForgeServer.Auth.Domain;

public static class RoleClaims
{
    public static readonly string[] AllAppRoles =
        Enum.GetValues<Permissions>()
            .Select(r => r.ToString())
            .ToArray();

    public static void AddAppRoles(this ClaimsIdentity identity, IEnumerable<Permissions> roles)
    {
        foreach (var role in roles.Distinct())
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, role.ToString()));
        }
    }

    public static void AddAppRoles(this ClaimsIdentity identity, IEnumerable<string> roleNames)
    {
        foreach (var name in roleNames.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct())
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, name));
        }
    }

    public static bool HasRealmAdmin(this ClaimsPrincipal principal) =>
        principal.IsInRole("REALM_ADMIN");
}