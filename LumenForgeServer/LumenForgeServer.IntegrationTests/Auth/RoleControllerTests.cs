// RoleControllerTests.cs
using System.Net;
using System.Text.Json;
using FluentAssertions;
using LumenForgeServer.Auth.Domain;
using LumenForgeServer.Auth.Dto.Views;
using LumenForgeServer.Common;
using LumenForgeServer.IntegrationTests.Collections;
using LumenForgeServer.IntegrationTests.Fixtures;

namespace LumenForgeServer.IntegrationTests.Auth;

/// <summary>
/// Integration tests for role catalog endpoints.
/// </summary>
[Collection(AuthCollection.Name)]
public class RoleControllerTests(AuthFixture fixture)
{
    [Fact]
    public async Task GET_roles_returns_catalog()
    {
        var adminBundle = await fixture.GetInitialAdminUserAsync();
        var resp = await adminBundle.AppClient.GetAsync("/api/v1/auth/roles");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var roles = JsonSerializer.Deserialize<List<RoleViewDto>>(
            await resp.Content.ReadAsStringAsync(),
            Json.GetJsonSerializerOptions());

        roles.Should().NotBeNull();
        roles.Should().Contain(r => r.Name == Permissions.DeviceRead.ToString());
        roles.Should().Contain(r => r.Value == (int)Permissions.DeviceRead);
    }
}