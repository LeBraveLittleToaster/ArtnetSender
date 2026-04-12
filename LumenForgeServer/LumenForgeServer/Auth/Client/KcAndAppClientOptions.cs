namespace LumenForgeServer.Auth.Client;

/// <summary>
/// Configuration settings used by Keycloak-aware integration test clients.
/// </summary>
public class KcAndAppClientOptions
{
    public string KcAdminPass { get; init; }
    public string KcAdminUser { get; init; }

    public string KcAdminRealm { get; init; }

    public string KcClientId { get; init; }

    public string KcRealm { get; init; }

    public string KcBaseUrl { get; init; }

    public string AppBaseUrl { get; init; }
    public string KcTestClientId
    { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="KcAndAppClientOptions"/> class.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    private KcAndAppClientOptions()
    {
    }

    /// <summary>
    /// Executes the from environment operation.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <returns>The operation result.</returns>
    public static KcAndAppClientOptions FromEnvironment()
    {
        return new KcAndAppClientOptions
        {
            AppBaseUrl = Environment.GetEnvironmentVariable("APP_BASE_URL")
                ?? Environment.GetEnvironmentVariable("APP_BASEURL")
                ?? "https://localhost:7217",
            KcBaseUrl = Environment.GetEnvironmentVariable("KC_BASEURL") ?? "http://localhost:8080",
            KcRealm = Environment.GetEnvironmentVariable("KC_REALM") ?? "lumenforge-realm",
            KcClientId = Environment.GetEnvironmentVariable("KC_CLIENTID") ?? "admin-cli",
            KcTestClientId = Environment.GetEnvironmentVariable("KC_TESTCLIENTID") ?? "lumenforge-test",

            KcAdminRealm = Environment.GetEnvironmentVariable("KC_ADMIN_REALM") ?? "master",
            KcAdminUser = Environment.GetEnvironmentVariable("KC_ADMIN_USER") ?? "admin",
            KcAdminPass = Environment.GetEnvironmentVariable("KC_ADMIN_PASS") ?? "adminpassword"

        };
    }
}
