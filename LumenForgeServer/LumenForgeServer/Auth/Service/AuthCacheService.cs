using Microsoft.Extensions.Caching.Memory;

namespace LumenForgeServer.Auth.Service
{
    public class AuthCacheService(IMemoryCache memoryCache, KcService kcService)
    {
        /// <summary>
        /// Executes the remove user roles from cache by user kc id operation.
        /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
        /// </summary>
        /// <remarks>Potential side effects: may persist state changes, emit workflow logs, or call external dependencies.</remarks>
        /// <param name="userKcId">Text input used by this operation.</param>
        /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task RemoveUserRolesFromCacheByUserKcId(string userKcId, CancellationToken ct)
        {
            memoryCache.Remove(AuthUtils.GetUserRoleCacheKeyForUserKc(userKcId));
            await Task.CompletedTask;
        }

        /// <summary>
        /// Executes the remove user roles from cache and logout by user kc id operation.
        /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
        /// </summary>
        /// <remarks>Potential side effects: may persist state changes, emit workflow logs, or call external dependencies.</remarks>
        /// <param name="userKcId">Text input used by this operation.</param>
        /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task RemoveUserRolesFromCacheAndLogoutByUserKcId(string userKcId, CancellationToken ct)
        {
            memoryCache.Remove(AuthUtils.GetUserRoleCacheKeyForUserKc(userKcId));
            await kcService.LogoutUserFromKeycloakByUserKcId(userKcId, ct);
        }
    }
}
