namespace LumenForgeServer.Auth.Domain;

public enum Policy
{
    GroupRoleAndUserRead,
    GroupUpdateReadUser,
    GroupUpdateRoleRead,
    UserAndRoleRead,
    UserReadOrOwnProfile
}
