using LumenForgeServer.Auth.Domain;
using LumenForgeServer.Auth.Dto;
using LumenForgeServer.Inventory.Domain;
using LumenForgeServer.Inventory.Dto.View;
using NodaTime;

namespace LumenForgeServer.Auth.Factory;

/// <summary>
/// Builds User domain objects from auth DTOs.
/// </summary>
public static class UserFactory
{
    /// <summary>
    /// Builds a <see cref="KcUserReference"/> instance from a user creation payload.
    /// </summary>
    /// <param name="dto">Payload containing the Keycloak subject identifier.</param>
    /// <returns>A new user instance with joined timestamp set.</returns>
    public static KcUserReference BuildUser(string userKcId, string username, string email, string firstname, string lastname)
    {
        return new KcUserReference
        {
            JoinedAt = SystemClock.Instance.GetCurrentInstant(),
            UserKcId = userKcId,
            UsernameMirror =  username,
            EmailMirror =  email,
            FirstNameMirror =  firstname,
            LastNameMirror =  lastname,
        };
    }
}
