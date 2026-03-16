using System.Security.Claims;
using System.Text.Json;
using LumenForgeServer.Auth.Domain;
using LumenForgeServer.Auth.Domain.Session;
using LumenForgeServer.Auth.Persistence;
using LumenForgeServer.Auth.Service;
using LumenForgeServer.Catalogue.Persistence;
using LumenForgeServer.Catalogue.Service;
using LumenForgeServer.Common.Database;
using LumenForgeServer.Common.Exceptions;
using LumenForgeServer.Inventory.Persistance;
using LumenForgeServer.Inventory.Service;
using LumenForgeServer.Maintenance.Persistence;
using LumenForgeServer.Maintenance.Service;
using LumenForgeServer.Rentals.Persistence;
using LumenForgeServer.Rentals.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using LumenForgeServer.Common.Database.Seeding;
using LumenForgeServer.Common.Database.Seeding.Seeders;
using Microsoft.AspNetCore.Routing;

namespace LumenForgeServer.Common;

/// <summary>
/// Registers infrastructure services with the dependency injection container.
/// </summary>
public static class DiRegistration
{
    /// <summary>
    /// Registers the EF Core database context.
    /// </summary>
    /// <param name="builder">Application builder that owns the service collection.</param>
    public static void RegisterDbContext(WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<AppDbContext>(opt =>
        {
            opt.UseNpgsql(builder.Configuration.GetConnectionString("DB_NAME"), 
                    o => o.UseNodaTime())
                .UseSnakeCaseNamingConvention();
        });
    }


    /// <summary>
    /// Registers the in-memory cache.
    /// </summary>
    /// <param name="builder">Application builder that owns the service collection.</param>
    public static void RegisterMemoryCache(WebApplicationBuilder builder)
    {
        builder.Services.AddMemoryCache();
    }

    /// <summary>
    /// Registers repository implementations.
    /// </summary>
    /// <param name="builder">Application builder that owns the service collection.</param>
    public static void RegisterRepositories(WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
        builder.Services.AddScoped<IAuthRepository, AuthRepository>();
        builder.Services.AddScoped<IMaintenanceRepository, MaintenanceRepository>();
        builder.Services.AddScoped<ICatalogueRepository, CatalogueRepository>();
        builder.Services.AddScoped<IRentalRepository, RentalRepository>();
        builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
    }

    /// <summary>
    /// Registers application services.
    /// </summary>
    /// <param name="builder">Application builder that owns the service collection.</param>
    public static void RegisterServices(WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<KcService>();
        builder.Services.AddScoped<GroupService>();
        builder.Services.AddScoped<CatalogueService>();
        builder.Services.AddScoped<CategoryService>();
        builder.Services.AddScoped<VendorService>();
        builder.Services.AddScoped<DeviceService>();
        builder.Services.AddScoped<UserService>();
        builder.Services.AddScoped<MaintenanceService>();
        builder.Services.AddScoped<RentalService>();
        builder.Services.AddScoped<ChecklistService>();
        builder.Services.AddScoped<QuestionService>();
    }

    /// <summary>
    /// Registers the data seeder pipeline.
    /// </summary>
    public static void RegisterSeeders(WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<DataSeederOrchestrator>();
        builder.Services.AddScoped<IDataSeeder, MaintenanceStatusSeeder>();
        builder.Services.AddScoped<IDataSeeder, VendorCategorySeeder>();
        builder.Services.AddScoped<IDataSeeder, DeviceSeeder>();
        builder.Services.AddScoped<IDataSeeder, CatalogueSeeder>();
        builder.Services.AddScoped<IDataSeeder, QuestionSeeder>();
        builder.Services.AddScoped<IDataSeeder, UserGroupSeeder>();
    }

    /// <summary>
    /// Registers ProblemDetails and exception handlers.
    /// </summary>
    /// <param name="builder">Application builder that owns the service collection.</param>
    public static void RegisterExceptionHandler(WebApplicationBuilder builder)
    {
        builder.Services.AddProblemDetails(configure =>
        {
            configure.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Extensions.TryAdd("requestId", context.HttpContext.TraceIdentifier);
            };
        });
        builder.Services.AddExceptionHandler<NotFoundExceptionHandler>();
        builder.Services.AddExceptionHandler<UniqueConstraintExceptionHandler>();
        builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    }

    /// <summary>
    /// Registers Swagger and OAuth2 definitions for Keycloak.
    /// </summary>
    /// <param name="builder">Application builder that owns the service collection.</param>
    /// <param name="keycloakAuthority">Keycloak base authority URL.</param>
    /// <param name="keycloakClientId">Keycloak client id for Swagger UI (currently unused).</param>
    /// <exception cref="UriFormatException">Thrown when the Keycloak authority is not a valid URI.</exception>
    public static void RegisterSwagger(WebApplicationBuilder builder, string keycloakAuthority, string keycloakClientId)
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "LumenForge API",
                Version = "v0.0.1"
            });

            options.AddSecurityDefinition(nameof(SecuritySchemeType.OAuth2), new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri($"{keycloakAuthority}/protocol/openid-connect/auth"),
                        TokenUrl = new Uri($"{keycloakAuthority}/protocol/openid-connect/token"),
                        Scopes = new Dictionary<string, string>
                        {
                            { "openid", "OpenID Connect scope" },
                            { "profile", "User profile" }
                        }
                    }
                }
            });

            options.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference(nameof(SecuritySchemeType.OAuth2), doc),
                    []
                }
            });
        });
    }

    /// <summary>
    /// Registers JWT authentication for Keycloak and adds role claims from both Keycloak and the database.
    /// </summary>
    /// <param name="builder">Application builder that owns the service collection.</param>
    public static void AddAuthenticationJwt(WebApplicationBuilder builder)
    {
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IKeycloakUser, KeycloakUser>();

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = builder.Configuration["Keycloak:Issuer"]!;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidIssuer = builder.Configuration["Keycloak:Issuer"],
                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = "preferred_username"
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async ctx =>
                    {
                        if (ctx.Principal?.Identity is not ClaimsIdentity identity) return;

                        var keycloakUserId = ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                        if (keycloakUserId is null) return;

                        var jti = ctx.Principal?.FindFirst("jti")?.Value ?? "no-jti";
                        var cacheKey = $"app-roles:{keycloakUserId}:{jti}";

                        var cache = ctx.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();
                        if (!cache.TryGetValue(cacheKey, out string[]? appRoleNames) || appRoleNames is null)
                        {
                            if (IsPrivilegedRole(["REALM_ADMIN", "REALM_OWNER"], identity))
                            {
                                appRoleNames = RoleClaims.AllAppRoles;
                            }
                            else
                            {
                                var userService = ctx.HttpContext.RequestServices.GetRequiredService<UserService>();
                                var dbRoles =
                                    await userService.GetRolesForKcId(keycloakUserId, ctx.HttpContext.RequestAborted);
                                appRoleNames = dbRoles.Select(r => r.ToString()).Distinct().ToArray();
                            }

                            cache.Set(cacheKey, appRoleNames, new MemoryCacheEntryOptions
                            {
                                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                            });
                        }

                        identity.AddClaims(appRoleNames.Select(x => new Claim(ClaimTypes.Role, x)));
                        Console.WriteLine("DEBUG LINE");
                    }
                };
                options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
            });
        builder.Services.AddAuthorization();
    }

    /// <summary>
    /// Adds role claims from Keycloak "realm_access" into the current identity.
    /// </summary>
    /// <param name="identity">Identity to add roles to.</param>
    /// <exception cref="JsonException">Thrown when the realm_access claim contains invalid JSON.</exception>
    private static bool IsPrivilegedRole(string[] keys, ClaimsIdentity identity)
    {
        var realmAccess = identity.FindFirst("realm_access")?.Value;
        if (string.IsNullOrWhiteSpace(realmAccess) || !realmAccess.TrimStart().StartsWith("{")) return false;
        using var doc = JsonDocument.Parse(realmAccess);
        if (!doc.RootElement.TryGetProperty("roles", out var roles) ||
            roles.ValueKind != JsonValueKind.Array) return false;
        return roles.EnumerateArray()
            .Select(x => x.GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Any(r => keys.Contains(r));
    }

    /// <summary>
    /// Registers authorization policies.
    /// </summary>
    /// <param name="builder">Application builder that owns the service collection.</param>
    /// <remarks>
    /// Policies can be found under Enum Policy, Roles in the Enum Role in Auth.Domain. 
    /// </remarks>
    public static void AddAuthorization(WebApplicationBuilder builder)
    {
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(nameof(Policy.GroupRoleAndUserRead),
                p => p.RequireRole(nameof(Permissions.GroupRead), nameof(Permissions.RoleRead),
                    nameof(Permissions.UserRead)));
            options.AddPolicy(nameof(Policy.GroupUpdateReadUser),
                p => p.RequireRole(nameof(Permissions.GroupUpdate), nameof(Permissions.UserRead)));
            options.AddPolicy(nameof(Policy.GroupUpdateRoleRead),
                p => p.RequireRole(nameof(Permissions.GroupUpdate), nameof(Permissions.RoleRead)));
            options.AddPolicy(nameof(Policy.UserAndRoleRead),
                p => p.RequireRole(nameof(Permissions.UserRead), nameof(Permissions.RoleRead)));
            options.AddPolicy(nameof(Policy.UserReadOrOwnProfile), p => p.RequireAssertion(ctx =>
                ctx.User.IsInRole(nameof(Permissions.UserRead)) || IsOwnProfileAccess(ctx)));
        });
    }

    private static bool IsOwnProfileAccess(AuthorizationHandlerContext ctx)
    {
        if (ctx.Resource is not HttpContext httpContext)
            return false;

        var routeUserId = httpContext.GetRouteValue("userKcId")?.ToString()
                          ?? httpContext.GetRouteValue("keycloakId")?.ToString();
        if (string.IsNullOrWhiteSpace(routeUserId))
            return false;

        var callerUserId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? ctx.User.FindFirstValue("sub");

        return !string.IsNullOrWhiteSpace(callerUserId)
               && string.Equals(routeUserId, callerUserId, StringComparison.Ordinal);
    }
}