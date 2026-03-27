using NodaTime;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Text.Json;

namespace LumenForgeServer.Auth.Client;

/// <summary>
/// Authenticated app client backed by a Keycloak access token for integration tests.
/// </summary>
public sealed class KcClient
{
    private readonly KcAndAppClientOptions _kcAndAppOptions;

    private KcClient(KcAndAppClientOptions kcAndAppOptions)
    {
        _kcAndAppOptions = kcAndAppOptions;
        AdminClient = new HttpClient
        {
            BaseAddress = new Uri(kcAndAppOptions.KcBaseUrl)
        };
    }

    public HttpClient AdminClient { get; init; }
    private JwtSecurityToken? AccessToken { get; set; }
    private string? AccessTokenString { get; set; }

    public static async Task<KcClient> GenerateKcClientWithAccessTokenAsync(KcAndAppClientOptions kcAndAppClientOptions, CancellationToken ct)
    {
        var kcClient = new KcClient(kcAndAppClientOptions);
        await kcClient.RequestAndAttachAdminTokenAsync(kcAndAppClientOptions, ct);
        return kcClient;
    }

    private async Task<bool> RequestAndAttachAdminTokenAsync(KcAndAppClientOptions kcAndAppClientOptions, CancellationToken ct)
    {
        var data = new Dictionary<string, string>
        {
            ["username"] = kcAndAppClientOptions.KcAdminUser,
            ["password"] = kcAndAppClientOptions.KcAdminPass,
            ["grant_type"] = "password",
            ["client_id"] = "admin-cli",
        };

        using var content = new FormUrlEncodedContent(data);

        try
        {
            var url = $"/realms/{kcAndAppClientOptions.KcAdminRealm}/protocol/openid-connect/token";
            var resp = await AdminClient.PostAsync(url, content, ct);

            var body = await resp.Content.ReadAsStringAsync(ct);
            var respJson = JsonSerializer.Deserialize<JsonElement>(body);
            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException(
                    $"Keycloak token request failed: {(int)resp.StatusCode} {resp.ReasonPhrase}\n{body}");


            var adminToken = respJson.GetProperty("access_token").GetString();

            AccessTokenString = respJson.GetProperty("access_token").GetString()!;
            AccessToken = new JwtSecurityTokenHandler().ReadJwtToken(AccessTokenString);

            if (adminToken != null)
            {
                AdminClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", AccessTokenString);
                return true;
            }

        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        return false;
    }

    public async Task RefreshTokenAsync(CancellationToken ct = default)
    {
        await RequestAndAttachAdminTokenAsync(_kcAndAppOptions, ct);
    }

    public async Task LogoutUserFromAllSessionsAsync(string realm, string userKcId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(realm))
            throw new ArgumentException("Realm must be provided.", nameof(realm));

        if (string.IsNullOrWhiteSpace(userKcId))
            throw new ArgumentException("User Keycloak id must be provided.", nameof(userKcId));

        var response = await AdminClient.PostAsync(
            $"/admin/realms/{Uri.EscapeDataString(realm)}/users/{Uri.EscapeDataString(userKcId)}/logout",
            content: null,
            ct);

        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync(ct);
        throw new InvalidOperationException(
            $"Failed to logout user '{userKcId}' in realm '{realm}': Http {(int)response.StatusCode} {body}");
    }

    public bool IsTokenExpired(Duration? skew = null)
    {
        if (AccessToken is null) return true;

        var nowUtc = SystemClock.Instance.GetCurrentInstant().ToDateTimeUtc();
        var buffer = (skew ?? Duration.FromMinutes(1)).ToTimeSpan();
        return AccessToken.ValidTo <= nowUtc.Add(buffer);
    }
}
