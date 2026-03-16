using LumenForgeServer.Common;
using LumenForgeServer.Common.Database.Seeding;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;

var builder = WebApplication.CreateBuilder(args);

var keycloakAuthority = builder.Configuration["Keycloak:Authority"]!;
var keycloakClientId = builder.Configuration["Keycloak:ClientId"]!;

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});



DiRegistration.RegisterMemoryCache(builder);
DiRegistration.AddAuthorization(builder);
DiRegistration.AddAuthenticationJwt(builder);
DiRegistration.RegisterDbContext(builder);
DiRegistration.RegisterRepositories(builder);
DiRegistration.RegisterServices(builder);
DiRegistration.RegisterSwagger(builder, keycloakAuthority, keycloakClientId);

DiRegistration.RegisterExceptionHandler(builder);
DiRegistration.RegisterSeeders(builder);

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
});

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    using var scope = app.Services.CreateScope();
    var orchestrator = scope.ServiceProvider.GetRequiredService<DataSeederOrchestrator>();
    await orchestrator.RunAsync(SeedEnvironment.Dev, CancellationToken.None);

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.OAuthClientId(keycloakClientId);
        options.OAuthUsePkce();
    });
}

app.UseHttpsRedirection();

app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

namespace LumenForgeServer
{
    /// <summary>
    /// Marker type used by integration tests to reference the API entry assembly.
    /// </summary>
    public partial class Program { }
}
