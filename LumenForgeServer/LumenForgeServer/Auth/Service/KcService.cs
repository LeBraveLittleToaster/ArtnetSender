using LumenForgeServer.Auth.Client;
using LumenForgeServer.Auth.Dto.Command;
using LumenForgeServer.Common.Exceptions;
using System.Net;
using System.Text.Json;

namespace LumenForgeServer.Auth.Service;

public class KcService
{
    private KcClient? _kcClient;
    private readonly KcAndAppClientOptions _kcAndAppOptions = KcAndAppClientOptions.FromEnvironment();
    private readonly SemaphoreSlim _lock = new(1, 1);

    private async Task EnsureInitializedAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (_kcClient == null)
                _kcClient = await KcClient.GenerateKcClientWithAccessTokenAsync(_kcAndAppOptions, CancellationToken.None);
            else if (_kcClient.IsTokenExpired())
            {
                await _kcClient.RefreshTokenAsync();
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<string> AddUserToKeycloak(AddKcUserDto dto, CancellationToken ct)
    {
        await EnsureInitializedAsync();

        var newUser = new
        {
            username = dto.Username,
            enabled = true,
            firstName = dto.FirstName,
            lastName = dto.LastName,
            email = dto.Email,
            emailVerified = true,
            groups = dto.Groups,
            realmRoles = dto.RealmRoles,
            credentials = new[]
            {
                new { type = "password", value = dto.Password, temporary = false }
            }
        };

        var response =
            await _kcClient!.AdminClient.PostAsJsonAsync($"/admin/realms/{_kcAndAppOptions.KcRealm}/users", newUser, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            if (response.StatusCode == HttpStatusCode.Conflict)
                throw new UniqueConstraintException("User already exists in Keycloak.", new Exception(body));

            throw new KeycloakException($"Failed to create user: Http {(int)response.StatusCode} {body}");
        }

        var location = response.Headers.Location?.ToString();

        if (string.IsNullOrEmpty(location))
            throw new KeycloakException("User created but Location header missing.");

        var userId = location.Split('/').Last();

        return userId;
    }

    public async Task DeleteUserFromKeycloakByUsername(string username, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username must be provided.", nameof(username));

        await EnsureInitializedAsync();

        var lookupResponse = await _kcClient!.AdminClient.GetAsync(
            $"/admin/realms/{_kcAndAppOptions.KcRealm}/users?username={Uri.EscapeDataString(username)}&exact=true",
            ct);

        if (!lookupResponse.IsSuccessStatusCode)
        {
            var body = await lookupResponse.Content.ReadAsStringAsync(ct);
            throw new KeycloakException(
                $"Failed to lookup user '{username}': Http {(int)lookupResponse.StatusCode} {body}");
        }

        var content = await lookupResponse.Content.ReadAsStringAsync(ct);

        using var document = JsonDocument.Parse(content);
        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
            throw new KeycloakException($"User '{username}' not found in Keycloak.");

        if (root.GetArrayLength() > 1)
            throw new KeycloakException($"Multiple users found for username '{username}'. Expected exactly one.");

        var userElement = root[0];

        if (!userElement.TryGetProperty("id", out var idElement) ||
            idElement.ValueKind != JsonValueKind.String)
            throw new KeycloakException($"User '{username}' found but id property missing.");

        var userId = idElement.GetString();

        var deleteResponse = await _kcClient.AdminClient.DeleteAsync(
            $"/admin/realms/{_kcAndAppOptions.KcRealm}/users/{userId}",
            ct);

        if (deleteResponse.IsSuccessStatusCode)
            return;

        var deleteBody = await deleteResponse.Content.ReadAsStringAsync(ct);

        throw new KeycloakException(
            $"Failed to delete user '{username}' (id: {userId}): Http {(int)deleteResponse.StatusCode} {deleteBody}");
    }

    public async Task LogoutUserFromKeycloakByUserKcId(string userKcId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userKcId))
            throw new ArgumentException("User Keycloak id must be provided.", nameof(userKcId));

        await EnsureInitializedAsync();

        try
        {
            await _kcClient!.LogoutUserFromAllSessionsAsync(_kcAndAppOptions.KcRealm, userKcId, ct);
        }
        catch (InvalidOperationException e)
        {
            throw new KeycloakException(e.Message);
        }
    }

    public async Task<int> DeleteUsersFromKeycloakByUsernamePrefix(string usernamePrefix, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(usernamePrefix))
            throw new ArgumentException("Prefix must be provided.", nameof(usernamePrefix));

        await EnsureInitializedAsync();

        var search = usernamePrefix.EndsWith('*') ? usernamePrefix[..^1] : usernamePrefix;

        const int pageSize = 100;
        var first = 0;
        var deleted = 0;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            var url =
                $"/admin/realms/{_kcAndAppOptions.KcRealm}/users" +
                $"?search={Uri.EscapeDataString(search)}" +
                $"&first={first}" +
                $"&max={pageSize}";

            var lookupResponse = await _kcClient!.AdminClient.GetAsync(url, ct);
            if (!lookupResponse.IsSuccessStatusCode)
            {
                var body = await lookupResponse.Content.ReadAsStringAsync(ct);
                throw new KeycloakException(
                    $"Failed to query users for '{usernamePrefix}': Http {(int)lookupResponse.StatusCode} {body}");
            }

            var json = await lookupResponse.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                throw new KeycloakException("Unexpected Keycloak response: expected JSON array.");

            var users = doc.RootElement;
            var count = users.GetArrayLength();
            if (count == 0)
                break;

            for (var i = 0; i < count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var user = users[i];

                if (!user.TryGetProperty("id", out var idEl) || idEl.ValueKind != JsonValueKind.String)
                    continue;

                if (!user.TryGetProperty("username", out var unEl) || unEl.ValueKind != JsonValueKind.String)
                    continue;

                var username = unEl.GetString() ?? string.Empty;
                if (!username.StartsWith(search, StringComparison.OrdinalIgnoreCase))
                    continue;

                var userId = idEl.GetString()!;
                var deleteResponse = await _kcClient.AdminClient.DeleteAsync(
                    $"/admin/realms/{_kcAndAppOptions.KcRealm}/users/{userId}",
                    ct);

                if (deleteResponse.IsSuccessStatusCode)
                {
                    deleted++;
                    continue;
                }

                var deleteBody = await deleteResponse.Content.ReadAsStringAsync(ct);

                if (deleteResponse.StatusCode == HttpStatusCode.NotFound)
                    continue;

                throw new KeycloakException(
                    $"Failed to delete user '{username}' (id: {userId}): Http {(int)deleteResponse.StatusCode} {deleteBody}");
            }

            first += pageSize;
        }

        return deleted;
    }
}