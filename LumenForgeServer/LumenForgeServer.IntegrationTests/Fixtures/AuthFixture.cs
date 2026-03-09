using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Azure;
using FluentAssertions;
using LumenForgeServer.Auth.Client;
using LumenForgeServer.Auth.Domain;
using LumenForgeServer.Auth.Dto.Command;
using LumenForgeServer.Auth.Dto.Views;
using LumenForgeServer.Common;
using LumenForgeServer.Common.Database;
using LumenForgeServer.Common.Exceptions;
using LumenForgeServer.IntegrationTests.Client;
using LumenForgeServer.IntegrationTests.TestSupport;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LumenForgeServer.IntegrationTests.Fixtures;

/// <summary>
/// Fixture for provisioning authenticated Keycloak test clients.
/// Also boots the in-memory API host so Program.cs runs (and DevDbSeeder runs in Development).
/// </summary>
public sealed class AuthFixture : IAsyncLifetime
{
    public static string UsernamePrefix = "test";

    private readonly KcAndAppClientOptions _kcAndAppOptions = KcAndAppClientOptions.FromEnvironment();

    // Host-related
    public WebApplicationFactory<LumenForgeServer.Program> Factory { get; private set; } = null!;
    public HttpClient AnonymousClient { get; private set; } = null!;

    // Auth-related
    private KcClient KcAdminUser = null!;
    private TestUserBundle AppAdminUser = null!;

    public Task InitializeHostAsync()
    {
        Factory = new WebApplicationFactory<LumenForgeServer.Program>()
            .WithWebHostBuilder(b => b.UseEnvironment("Development")); // required for IsDevelopment() seeding

        // Boots host -> Program.cs runs -> DevDbSeeder runs (inside IsDevelopment()).
        AnonymousClient = Factory.CreateClient();

        return Task.CompletedTask;
    }

    public async Task InitializeAsync()
    {
        // 1) Boot API host first (ensures Program.cs + seeding ran)
        await InitializeHostAsync();

        // 2) Then do Keycloak/admin provisioning
        KcAdminUser = await KcClient.GenerateKcClientWithAccessTokenAsync(_kcAndAppOptions, CancellationToken.None);
        AppAdminUser = await GetInitialAdminUserAsync();
    }

    public HttpClient GetAnonymousClient() => Factory.CreateClient();

    public async Task<TestUserBundle> GetInitialAdminUserAsync()
    {
        var apiHttpClient = Factory.CreateClient();

        var (accessTokenString, accessToken) = await GetKcTokenRequestAsync(
            _kcAndAppOptions,
            DbInitConstants.InitUsername,
            DbInitConstants.InitPassword);

        apiHttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessTokenString);

        return new TestUserBundle(accessToken, apiHttpClient);
    }

    public async Task<TestUserBundle> CreateNewUserAsync(CreateTestUserDto dto)
    {
        var apiHttpClient = Factory.CreateClient();

        var response = await apiHttpClient.PutAsJsonAsync("/api/v1/auth/users", dto);
        response.EnsureSuccessStatusCode();

        var (accessTokenString, accessToken) =
            await GetKcTokenRequestAsync(_kcAndAppOptions, dto.Username, dto.Password);

        apiHttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessTokenString);

        return new TestUserBundle(accessToken, apiHttpClient);
    }

    public async Task<TestUserBundle> CreateNewUserWithRolesAsync(CreateTestUserDto dto, Permissions[] roles)
    {
        var apiHttpClient = Factory.CreateClient();

        var response = await apiHttpClient.PutAsJsonAsync("/api/v1/auth/users", dto);
        if (response.StatusCode != HttpStatusCode.Created)
            throw new RequestFailedException(response.StatusCode.ToString());

        var (accessTokenString, accessToken) =
            await GetKcTokenRequestAsync(_kcAndAppOptions, dto.Username, dto.Password);

        apiHttpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessTokenString);

        var guid = Guid.NewGuid().ToString("N");
        var groupView = await CreateGroupWithRolesAsync(
            AppAdminUser.AppClient,
            roles,
            $"TestGroup{guid}",
            "Test description" + guid);

        var assignedResp = await AssignUserToGroupAsync(AppAdminUser.AppClient, groupView.Guid, accessToken.Subject);
        Console.WriteLine(assignedResp);

        return new TestUserBundle(accessToken, apiHttpClient);
    }

    private async Task<(string accessTokenString, JwtSecurityToken accessToken)> GetKcTokenRequestAsync(
        KcAndAppClientOptions options,
        string username,
        string password)
    {
        using var kcHttpClient = new HttpClient { BaseAddress = new Uri(options.KcBaseUrl) };

        var data = new Dictionary<string, string>
        {
            ["username"] = username,
            ["password"] = password,
            ["grant_type"] = "password",
            ["client_id"] = options.KcTestClientId,
        };

        using var content = new FormUrlEncodedContent(data);

        var url = $"/realms/{options.KcRealm}/protocol/openid-connect/token";
        var resp = await kcHttpClient.PostAsync(url, content, CancellationToken.None);

        var body = await resp.Content.ReadAsStringAsync(CancellationToken.None);
        var respJson = JsonSerializer.Deserialize<JsonElement>(body);

        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"Keycloak token request failed: {(int)resp.StatusCode} {resp.ReasonPhrase}\n{body}");

        var accessTokenString = respJson.GetProperty("access_token").GetString();
        if (string.IsNullOrWhiteSpace(accessTokenString))
            throw new KeycloakException("Failed to get access token");

        var accessToken = new JwtSecurityTokenHandler().ReadJwtToken(accessTokenString);
        return (accessTokenString, accessToken);
    }

    public async Task<GroupView> CreateGroupAsync(HttpClient adminClient, string? name = null,
        string? description = null)
    {
        var groupName = name ?? "Test Group " + Guid.NewGuid();
        var groupDesc = description ?? "Test Group Description " + Guid.NewGuid() + " Extended";

        var response = await adminClient.PutAsJsonAsync("/api/v1/auth/groups", new AddGroupDto
        {
            Name = groupName,
            Description = groupDesc,
            Roles = []
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var groupView = await JsonSerializer.DeserializeAsync<GroupView>(
            await response.Content.ReadAsStreamAsync(),
            Json.GetJsonSerializerOptions());

        groupView.Should().NotBeNull();
        return groupView!;
    }

    public async Task<GroupView> CreateGroupWithRolesAsync(
        HttpClient adminClient,
        IEnumerable<Permissions> roles,
        string name,
        string description)
    {
        var group = await CreateGroupAsync(adminClient, name, description);


        var assignResp = await adminClient.PutAsJsonAsync($"/api/v1/auth/groups/{group.Guid}/roles", new AssignGroupRolesDto()
        {
            Roles = roles.Distinct().ToArray()
        },CancellationToken.None);
        assignResp.StatusCode.Should().Be(HttpStatusCode.NoContent);


        return group;
    }

    public Task<HttpResponseMessage> AssignUserToGroupAsync(
        HttpClient adminClient,
        Guid groupGuid,
        string userKcId,
        string? assigneeKcId = null)
    {
        return adminClient.PutAsJsonAsync($"/api/v1/auth/groups/{groupGuid}/users", new AssignUserToGroupDto
        {
            assigneeKcId = assigneeKcId,
            userKcId = userKcId
        });
    }

    public async Task DisposeAsync()
    {
        AnonymousClient?.Dispose();
        if (Factory is not null) await Factory.DisposeAsync();
    }
}