using LumenForgeServer.Auth.Domain;
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
    /// <param name="userKcId">Text input used by this operation.</param>
    /// <param name="username">Text input used by this operation.</param>
    /// <param name="email">Text input used by this operation.</param>
    /// <param name="firstname">Text input used by this operation.</param>
    /// <param name="lastname">Text input used by this operation.</param>
    /// <returns>A new user instance with joined timestamp set.</returns>
    public static KcUserReference BuildUser(string userKcId, string username, string email, string firstname, string lastname)
    {
        return new KcUserReference
        {
            JoinedAt = SystemClock.Instance.GetCurrentInstant(),
            UserKcId = userKcId,
            UsernameMirror = username,
            EmailMirror = email,
            FirstNameMirror = firstname,
            LastNameMirror = lastname,
        };
    }
}
