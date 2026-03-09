using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace LumenForgeServer.Auth.Domain;

public static class RoleClaims
{
    public static readonly string[] AllAppRoles =
        Enum.GetValues<Permissions>()
            .Where(r => r != Permissions.None)
            .Select(r => r.ToString())
            .ToArray();

    public static void AddAppRoles(this ClaimsIdentity identity, IEnumerable<Permissions> roles)
    {
        foreach (var role in roles.Where(r => r != Permissions.None).Distinct())
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