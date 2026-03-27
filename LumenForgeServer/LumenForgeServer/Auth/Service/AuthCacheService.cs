using Microsoft.Extensions.Caching.Memory;

namespace LumenForgeServer.Auth.Service
{
    public class AuthCacheService(IMemoryCache memoryCache, KcService kcService)
    {
        public async Task RemoveUserRolesFromCacheByUserKcId(string userKcId, CancellationToken ct)
        {
            memoryCache.Remove(AuthUtils.GetUserRoleCacheKeyForUserKc(userKcId));
            await Task.CompletedTask;
        }

        public async Task RemoveUserRolesFromCacheAndLogoutByUserKcId(string userKcId, CancellationToken ct)
        {
            memoryCache.Remove(AuthUtils.GetUserRoleCacheKeyForUserKc(userKcId));
            await kcService.LogoutUserFromKeycloakByUserKcId(userKcId, ct);
        }
    }
}
