using FluentAssertions;
using LumenForgeServer.Common;
using LumenForgeServer.IntegrationTests.TestSupport;
using LumenForgeServer.Maintenance.Dto.Command;
using LumenForgeServer.Maintenance.Dto.View;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace LumenForgeServer.IntegrationTests.Maintenance;

/// <summary>
/// Shared helper methods for maintenance integration tests.
/// </summary>
internal static class MaintenanceTestHelpers
{
    public static async Task<MaintenanceStatusView> CreateStatusAsync(
        TestUserBundle userBundle,
        string? name = null,
        string? description = null)
    {
        var response = await userBundle.AppClient.PutAsJsonAsync("/api/v1/maintenance/statuses", new CreateMaintenanceStatusDto
        {
            Name = name ?? $"Status-{Guid.NewGuid():N}",
            Description = description ?? "Test status description",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await DeserializeAsync<MaintenanceStatusView>(response);
    }

    public static async Task<MaintenanceBacklogView> CreateBacklogAsync(
        TestUserBundle userBundle,
        Guid statusUuid,
        Guid? deviceUuid = null,
        string? issueSummary = null,
        decimal quantityAffected = 1m)
    {
        var response = await userBundle.AppClient.PutAsJsonAsync("/api/v1/maintenance/backlogs", new CreateMaintenanceBacklogDto
        {
            StatusUuid = statusUuid,
            DeviceUuid = deviceUuid,
            IssueSummary = issueSummary ?? $"Issue-{Guid.NewGuid():N}",
            IssueDescription = "Detailed description of the issue.",
            QuantityAffected = quantityAffected,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await DeserializeAsync<MaintenanceBacklogView>(response);
    }

    public static async Task<T> DeserializeAsync<T>(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        var value = JsonSerializer.Deserialize<T>(body, Json.GetJsonSerializerOptions());
        value.Should().NotBeNull();
        return value!;
    }
}
