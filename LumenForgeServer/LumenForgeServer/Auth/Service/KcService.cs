using LumenForgeServer.Auth.Client;
using LumenForgeServer.Auth.Dto.Command;
using LumenForgeServer.Common.Exceptions;
using System.Net;
using System.Text.Json;

namespace LumenForgeServer.Auth.Service;

public class KcService
{
    private KcClient? _kcClient;
    /// <summary>
    /// Executes the kc and app client options.from environment operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <returns>The operation result.</returns>
    private readonly KcAndAppClientOptions _kcAndAppOptions = KcAndAppClientOptions.FromEnvironment();
    /// <summary>
    /// Executes the new operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <returns>The operation result.</returns>
    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>
    /// Executes the ensure initialized async operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: may persist state changes, emit workflow logs, or call external dependencies.</remarks>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

    /// <summary>
    /// Executes the add user to keycloak operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: may persist state changes, emit workflow logs, or call external dependencies.</remarks>
    /// <param name="dto">Request payload containing the input data required for the operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the string result.</returns>
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

    /// <summary>
    /// Executes the delete user from keycloak by username operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: may persist state changes, emit workflow logs, or call external dependencies.</remarks>
    /// <param name="username">Text input used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

    /// <summary>
    /// Executes the logout user from keycloak by user kc id operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: may persist state changes, emit workflow logs, or call external dependencies.</remarks>
    /// <param name="userKcId">Text input used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
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

    /// <summary>
    /// Executes the delete users from keycloak by username prefix operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: may persist state changes, emit workflow logs, or call external dependencies.</remarks>
    /// <param name="usernamePrefix">Text input used by this operation.</param>
    /// <param name="ct">Cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that resolves to the int result.</returns>
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