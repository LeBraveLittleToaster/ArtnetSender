using System.IdentityModel.Tokens.Jwt;

namespace LumenForgeServer.IntegrationTests.TestSupport;

public record TestUserBundle(JwtSecurityToken JwtSecurityToken, HttpClient AppClient)
{
    public string GetKcUserId()
    {
        return JwtSecurityToken.Subject;
    }
}