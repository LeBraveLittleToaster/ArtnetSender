namespace LumenForgeServer.Auth
{
    public class AuthUtils
    {
        public static String GetUserRoleCacheKeyForUserKc(string userKcId)
        {
            return "app-roles:" + userKcId;
        }
    }
}
