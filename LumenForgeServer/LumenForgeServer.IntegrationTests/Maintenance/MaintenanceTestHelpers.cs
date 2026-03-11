using FluentAssertions;
using LumenForgeServer.Common;
using LumenForgeServer.IntegrationTests.TestSupport;
using LumenForgeServer.Maintenance.Dto.Command;
using LumenForgeServer.Maintenance.Dto.View;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace LumenForgeServer.IntegrationTests.Maintenance;

internal static class MaintenanceTestHelpers
{
    public static async Task<MaintenanceJobView> CreateJobAsync(
        TestUserBundle user,
        IReadOnlyList<Guid> deviceGuids,
        string? name = null)
    {
        var response = await user.AppClient.PutAsJsonAsync("/api/v1/maintenance/jobs", new CreateMaintenanceJobDto
        {
            Name = name ?? $"Job-{Guid.NewGuid():N}",
            Description = "Issue report details",
            DeviceGuids = deviceGuids,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await DeserializeAsync<MaintenanceJobView>(response);
    }

    public static async Task<MaintenanceTaskView> CreateTaskAsync(
        TestUserBundle user,
        Guid jobGuid,
        string? description = null)
    {
        var response = await user.AppClient.PostAsJsonAsync($"/api/v1/maintenance/jobs/{jobGuid}/tasks", new CreateMaintenanceTaskDto
        {
            Description = description ?? $"Task-{Guid.NewGuid():N}",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await DeserializeAsync<MaintenanceTaskView>(response);
    }

    public static async Task<T> DeserializeAsync<T>(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        var payload = JsonSerializer.Deserialize<T>(body, Json.GetJsonSerializerOptions());
        payload.Should().NotBeNull();
        return payload!;
    }
}
