namespace LumenForgeServer.Auth
{
    /// <summary>
    /// Utility helpers for authentication/authorization cross-cutting concerns.
    /// </summary>
    public class AuthUtils
    {
        /// <summary>
        /// Builds the cache key used to store application roles for a user.
        /// Core concept: cache entries are namespaced with a stable prefix and indexed by Keycloak subject id.
        /// </summary>
        /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
        /// <param name="userKcId">Keycloak subject identifier of the user.</param>
        /// <returns>Cache key string used by auth role caching components.</returns>
        public static String GetUserRoleCacheKeyForUserKc(string userKcId)
        {
            return "app-roles:" + userKcId;
        }
    }
}
