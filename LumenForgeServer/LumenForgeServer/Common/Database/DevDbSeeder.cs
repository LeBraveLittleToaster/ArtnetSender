using LumenForgeServer.Auth.Domain;
using LumenForgeServer.Auth.Dto.Command;
using LumenForgeServer.Auth.Dto.Views;
using LumenForgeServer.Auth.Service;
using LumenForgeServer.Common.Exceptions;
using LumenForgeServer.Inventory.Dto.Create;
using LumenForgeServer.Inventory.Domain;
using LumenForgeServer.Inventory.Service;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using NodaTime;

namespace LumenForgeServer.Common.Database;

/// <summary>
/// Utility to reset and seed the development database with sample data.
/// </summary>
public static class DevDbSeeder
{
    /// <summary>
    /// Deletes the database, recreates it, and inserts minimal seed data.
    /// </summary>
    /// <param name="serviceProvider">Service provider used to resolve scoped services.</param>
    /// <returns>A task that completes when seeding is finished.</returns>
    /// <remarks>
    /// This uses <c>EnsureDeletedAsync</c> and <c>EnsureCreatedAsync</c> rather than migrations.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when required services are missing from the service provider.
    /// </exception>
    /// <exception cref="Exception">
    /// Re-throws any exception that occurs during delete, create, or seed operations.
    /// </exception>
    public static async Task DeleteAndSeedDbAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("DevDbSeeder");
        ;

        var db = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var kcService = scope.ServiceProvider.GetRequiredService<KcService>();
        var userService = scope.ServiceProvider.GetRequiredService<UserService>();
        var groupService = scope.ServiceProvider.GetRequiredService<GroupService>();


        var vendorService = scope.ServiceProvider.GetRequiredService<VendorService>();
        var categoryService = scope.ServiceProvider.GetRequiredService<CategoryService>();

        try
        {
            logger.LogWarning("Starting development database reset.");

            logger.LogInformation("Deleting database...");
            await db.Database.EnsureDeletedAsync();

            logger.LogInformation("Creating database without migrations...");
            await db.Database.EnsureCreatedAsync();

            logger.LogInformation("Seeding dummy data...");

            logger.LogInformation("Deleting all test* named users");
            await DeleteAllTestUsers(kcService);

            logger.LogInformation("Creating default admin group...");
            var groupView = await CreateDefaultGroup(groupService);
            logger.LogInformation("Creating default admin user...");
            var initialAdminUser = await CreateInitialAdminUser(userService, kcService);
            logger.LogInformation("Assigning admin user to admin group...");
            await AssignAdminUserToGroup(groupView, initialAdminUser, groupService);            

            
            await vendorService.CreateVendor(new CreateVendorDto { Name = "Some Cool Vendor" }, CancellationToken.None);

            for (var i = 0; i < 10; i++)
            {
                await categoryService.CreateCategory(
                    new CreateCategoryDto { Name = "Some Category " + i, Description = "Description " + i },
                    CancellationToken.None);
            }

            if (!db.MaintenanceStatuses.Any())
            {
                var now = SystemClock.Instance.GetCurrentInstant();
                db.MaintenanceStatuses.Add(new MaintenanceStatus
                {
                    Uuid = Guid.CreateVersion7(),
                    Name = "Operational",
                    Description = "Default maintenance status.",
                    CreatedAt = now,
                    UpdatedAt = now
                });
                await db.SaveChangesAsync();
                logger.LogInformation("Seeded default maintenance status.");
            }

            logger.LogInformation("Development database reset and seeding completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while resetting and seeding the development database.");
            throw;
        }
    }

    private static async Task AssignAdminUserToGroup(GroupView groupView, string kcUserId, GroupService groupService)
    {
        await groupService.AssignUserToGroup(null, kcUserId, groupView.Guid,  CancellationToken.None);
    }

    private static async Task<string> CreateInitialAdminUser(UserService userService, KcService kcService)
    {
        var dto = new AddKcUserDto()
        {
            FirstName = DbInitConstants.InitFirstName,
            LastName = DbInitConstants.InitLastName,
            Email = DbInitConstants.InitEmail,
            Password = DbInitConstants.InitPassword,
            Username = DbInitConstants.InitUsername
        };
        string? kcUserId = null;
        try
        {
            kcUserId = await kcService.AddUserToKeycloak(dto, CancellationToken.None);
        }
        catch (UniqueConstraintException e)
        {
            await kcService.DeleteUserFromKeycloakByUsername(dto.Username, CancellationToken.None);
            kcUserId = await kcService.AddUserToKeycloak(dto, CancellationToken.None);
        }
        await userService.AddUser(kcUserId, dto, CancellationToken.None);
        return kcUserId;

    }

    private static async Task<GroupView> CreateDefaultGroup(GroupService groupService)
    {
        var groupView = await groupService.AddGroup(new AddGroupDto()
        {
            Description = DbInitConstants.InitAdminGroupDescription,
            Name = DbInitConstants.InitAdminGroupName,
            Roles = Enum.GetValues<Role>()
        },  CancellationToken.None);
        return groupView;
        
    }

    private static async Task DeleteAllTestUsers(KcService kcService)
    {
        await kcService.DeleteUsersFromKeycloakByUsernamePrefix("test", CancellationToken.None);
    }
}