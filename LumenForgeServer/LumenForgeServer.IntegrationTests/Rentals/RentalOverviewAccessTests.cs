using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using LumenForgeServer.Auth.Domain;
using LumenForgeServer.Common;
using LumenForgeServer.IntegrationTests.Collections;
using LumenForgeServer.IntegrationTests.Fixtures;
using LumenForgeServer.IntegrationTests.TestSupport;
using LumenForgeServer.Rentals.Dto.Command;
using LumenForgeServer.Rentals.Dto.Query;
using LumenForgeServer.Rentals.Dto.View;
using NodaTime;

namespace LumenForgeServer.IntegrationTests.Rentals;

[Collection(AuthCollection.Name)]
public class RentalOverviewAccessTests(AuthFixture fixture)
{
    [Fact]
    public async Task Admin_can_read_all_rentals_and_roleless_user_can_create_and_read_own()
    {
        var admin = await fixture.GetInitialAdminUserAsync();
        var userA = await fixture.CreateNewUserAsync(CreateTestUserDto.CreateTestUser());
        var userB = await fixture.CreateNewUserAsync(CreateTestUserDto.CreateTestUser());

        await AssertUserHasNoRolesAsync(admin, userA.GetKcUserId());

        var userAProcessGuid = await CreateRentalAsync(userA);
        var userBProcessGuid = await CreateRentalAsync(userB);

        var userAListResponse = await userA.AppClient.GetAsync("/api/v1/rentals?limit=100&offset=0");
        userAListResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var userAList = await ReadRentalListAsync(userAListResponse);
        userAList.Select(x => x.Guid).Should().Contain(userAProcessGuid);
        userAList.Select(x => x.Guid).Should().NotContain(userBProcessGuid);

        var adminListResponse = await admin.AppClient.GetAsync("/api/v1/rentals?limit=200&offset=0");
        adminListResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var adminList = await ReadRentalListAsync(adminListResponse);
        adminList.Select(x => x.Guid).Should().Contain(userAProcessGuid);
        adminList.Select(x => x.Guid).Should().Contain(userBProcessGuid);
    }

    private static async Task AssertUserHasNoRolesAsync(TestUserBundle admin, string userKcId)
    {
        var rolesResponse = await admin.AppClient.GetAsync($"/api/v1/auth/users/{userKcId}/roles");
        rolesResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var rolesJson = await rolesResponse.Content.ReadAsStringAsync();
        var roles = JsonSerializer.Deserialize<List<Permissions>>(rolesJson, Json.GetJsonSerializerOptions());
        roles.Should().NotBeNull();
        roles.Should().BeEmpty();
    }

    private static async Task<Guid> CreateRentalAsync(TestUserBundle user)
    {
        var questionGuid = await GetAnyQuestionGuidAsync(user);
        var now = SystemClock.Instance.GetCurrentInstant();

        var response = await user.AppClient.PostAsJsonAsync(
            "/api/v1/rentals/actions/create",
            new CreateRentalDto
            {
                CustomerName = $"Customer-{Guid.NewGuid():N}",
                CustomerEmail = $"customer-{Guid.NewGuid():N}@example.com",
                Purpose = "Integration test rental",
                RequestedStart = now + Duration.FromDays(1),
                RequestedEnd = now + Duration.FromDays(3),
                Notes = "Created in integration test",
                QASets =
                [
                    new QASet
                    {
                        Guid = questionGuid.ToString(),
                        Value = "Yes"
                    }
                ]
            },
            Json.GetJsonSerializerOptions());

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<CreateRentalResultView>(body, Json.GetJsonSerializerOptions());
        result.Should().NotBeNull();
        result!.ProcessInstanceGuid.Should().NotBe(Guid.Empty);
        return result.ProcessInstanceGuid;
    }

    private static async Task<Guid> GetAnyQuestionGuidAsync(TestUserBundle user)
    {
        const string requestBody =
            """
            {
              "event_name": "Rental Test Event",
              "event_description": "Rental integration test input",
              "event_start_date": "2026-04-01T10:00:00Z",
              "event_end_date": "2026-04-02T10:00:00Z",
              "event_location": "Berlin"
            }
            """;

        var response = await user.AppClient.PostAsync(
            "/api/v1/rentals/questions",
            new StringContent(requestBody, Encoding.UTF8, "application/json"));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        var payload = JsonSerializer.Deserialize<RentalQuestionsDto>(body, Json.GetJsonSerializerOptions());
        payload.Should().NotBeNull();
        payload!.Questions.Should().NotBeEmpty();
        return payload.Questions[0].Guid;
    }

    private static async Task<List<RentalProcessSummaryView>> ReadRentalListAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);
        var list = json.RootElement.GetProperty("list")
            .Deserialize<List<RentalProcessSummaryView>>(Json.GetJsonSerializerOptions());
        list.Should().NotBeNull();
        return list!;
    }
}
