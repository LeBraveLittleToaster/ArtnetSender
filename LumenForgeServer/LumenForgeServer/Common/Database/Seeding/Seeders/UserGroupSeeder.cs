using LumenForgeServer.Auth.Domain;
using LumenForgeServer.Auth.Dto.Command;
using LumenForgeServer.Auth.Dto.Views;
using LumenForgeServer.Auth.Service;
using LumenForgeServer.Common.Exceptions;

namespace LumenForgeServer.Common.Database.Seeding.Seeders;

/// <summary>
/// Cleans up Keycloak test users then seeds the admin user, admin group, and dummy test users/groups. Dev only.
/// </summary>
public class UserGroupSeeder(UserService userService, GroupService groupService, KcService kcService) : IDataSeeder
{
    public int Order => 60;
    public SeedEnvironment Environment => SeedEnvironment.Dev;

    /// <summary>
    /// Executes the seed async operation.
    /// </summary>
    /// <remarks>Potential side effects: may modify state as part of this operation.</remarks>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task SeedAsync(CancellationToken ct)
    {
        await kcService.DeleteUsersFromKeycloakByUsernamePrefix("test", ct);

        var adminGroup = await CreateAdminGroup(ct);
        var adminKcId  = await CreateInitialAdminUser(ct);
        await groupService.AssignUserToGroup(null, adminKcId, adminGroup.Guid, ct);

        await CreateTestUsersAndGroups(25, 7, ct);
    }

    /// <summary>
    /// Executes the create admin group operation.
    /// </summary>
    /// <remarks>Potential side effects: may modify state as part of this operation.</remarks>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the GroupView result.</returns>
    private async Task<GroupView> CreateAdminGroup(CancellationToken ct)
        => await groupService.AddGroup(new AddGroupDto
        {
            Name        = DbInitConstants.InitAdminGroupName,
            Description = DbInitConstants.InitAdminGroupDescription,
            Roles       = Enum.GetValues<Permissions>(),
        }, ct);

    /// <summary>
    /// Executes the create initial admin user operation.
    /// </summary>
    /// <remarks>Potential side effects: may modify state as part of this operation.</remarks>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the string result.</returns>
    private async Task<string> CreateInitialAdminUser(CancellationToken ct)
    {
        var dto = new AddKcUserDto
        {
            FirstName = DbInitConstants.InitFirstName,
            LastName  = DbInitConstants.InitLastName,
            Email     = DbInitConstants.InitEmail,
            Password  = DbInitConstants.InitPassword,
            Username  = DbInitConstants.InitUsername,
        };

        string kcUserId;
        try
        {
            kcUserId = await kcService.AddUserToKeycloak(dto, ct);
        }
        catch (UniqueConstraintException)
        {
            await kcService.DeleteUserFromKeycloakByUsername(dto.Username, ct);
            kcUserId = await kcService.AddUserToKeycloak(dto, ct);
        }

        await userService.AddUser(kcUserId, dto, ct);
        return kcUserId;
    }

    /// <summary>
    /// Executes the create test users and groups operation.
    /// </summary>
    /// <remarks>Potential side effects: may modify state as part of this operation.</remarks>
    /// <param name="userCount">Numeric input used by this operation.</param>
    /// <param name="groupCount">Numeric input used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task CreateTestUsersAndGroups(int userCount, int groupCount, CancellationToken ct)
    {
        var defaultGroup = await groupService.AddGroup(new AddGroupDto
        {
            Name        = "Test Default G0",
            Description = "Test Default Description of Group 0",
            Roles       = [],
        }, ct);

        var groups = new List<GroupView>();
        for (var i = 1; i <= groupCount; i++)
        {
            var group = await groupService.AddGroup(new AddGroupDto
            {
                Name        = $"TestG{i}",
                Description = $"Test Description of Group {i}",
                Roles       = [.. Enum.GetValues<Permissions>().Where(_ => Random.Shared.Next(2) > 0)],
            }, ct);
            groups.Add(group);
        }

        for (var i = 0; i < userCount; i++)
        {
            var dto = new AddKcUserDto
            {
                Email     = $"test{i}@test.de",
                FirstName = $"Ftest{i}",
                LastName  = $"Ltest{i}",
                Username  = $"testuser{i}",
                Password  = "testuser",
            };

            var kcId = await kcService.AddUserToKeycloak(dto, ct);
            await userService.AddUser(kcId, dto, ct);
            await groupService.AssignUserToGroup(null, kcId, defaultGroup.Guid, ct);

            foreach (var group in groups.Where(_ => Random.Shared.Next(2) > 0))
            {
                await groupService.AssignUserToGroup(null, kcId, group.Guid, ct);
            }
        }
    }
}
