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
using LumenForgeServer.Rentals.Service.Actions;
using NodaTime;

namespace LumenForgeServer.IntegrationTests.Rentals;

[Collection(AuthCollection.Name)]
public class RentalActionAuthorizationTests(AuthFixture fixture)
{
    [Fact]
    public async Task OwnScopeUser_DoesNotSeeOrExecuteApproveRequestOnOwnRental()
    {
        var owner = await fixture.CreateNewUserWithRolesAsync(
            CreateTestUserDto.CreateTestUser(),
            [
                Permissions.RentalUserOwn,
                Permissions.RentalActionCreateRental,
                Permissions.RentalActionApproveRequest
            ]);

        var processGuid = await CreateRentalAsync(owner);

        var availableResponse = await owner.AppClient.GetAsync($"/api/v1/rentals/actions/{processGuid}/available");
        availableResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var availableBody = await availableResponse.Content.ReadAsStringAsync();
        var actions = JsonSerializer.Deserialize<List<RentalActionType>>(availableBody, Json.GetJsonSerializerOptions());
        actions.Should().NotBeNull();
        actions!.Should().NotContain(RentalActionType.ApproveRequest);

        var approveResponse = await owner.AppClient.PostAsJsonAsync(
            $"/api/v1/rentals/actions/{processGuid}/approve-request",
            new ApproveRequestDto { Comment = "owner tries approval" },
            Json.GetJsonSerializerOptions());

        approveResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task NonOwnerWithGlobalUpdateAndApprovePermission_CanApproveRequest()
    {
        var owner = await fixture.CreateNewUserWithRolesAsync(
            CreateTestUserDto.CreateTestUser(),
            [
                Permissions.RentalUserOwn,
                Permissions.RentalActionCreateRental
            ]);
        var approver = await fixture.CreateNewUserWithRolesAsync(
            CreateTestUserDto.CreateTestUser(),
            [
                Permissions.RentalReadAll,
                Permissions.RentalUpdateAll,
                Permissions.RentalActionApproveRequest
            ]);

        var processGuid = await CreateRentalAsync(owner);

        var availableResponse = await approver.AppClient.GetAsync($"/api/v1/rentals/actions/{processGuid}/available");
        availableResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var availableActions = JsonSerializer.Deserialize<List<RentalActionType>>(
            await availableResponse.Content.ReadAsStringAsync(),
            Json.GetJsonSerializerOptions());

        availableActions.Should().NotBeNull();
        availableActions!.Should().Contain(RentalActionType.ApproveRequest);

        var approveResponse = await approver.AppClient.PostAsJsonAsync(
            $"/api/v1/rentals/actions/{processGuid}/approve-request",
            new ApproveRequestDto { Comment = "approved by manager" },
            Json.GetJsonSerializerOptions());

        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateAllUser_CanApproveOwnRequest_WithoutActionSpecificPermission()
    {
        var adminLikeUser = await fixture.CreateNewUserWithRolesAsync(
            CreateTestUserDto.CreateTestUser(),
            [
                Permissions.RentalReadAll,
                Permissions.RentalUpdateAll
            ]);

        var processGuid = await CreateRentalAsync(adminLikeUser);

        var availableResponse = await adminLikeUser.AppClient.GetAsync($"/api/v1/rentals/actions/{processGuid}/available");
        availableResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var availableActions = JsonSerializer.Deserialize<List<RentalActionType>>(
            await availableResponse.Content.ReadAsStringAsync(),
            Json.GetJsonSerializerOptions());

        availableActions.Should().NotBeNull();
        availableActions!.Should().Contain(RentalActionType.ApproveRequest);

        var approveResponse = await adminLikeUser.AppClient.PostAsJsonAsync(
            $"/api/v1/rentals/actions/{processGuid}/approve-request",
            new ApproveRequestDto { Comment = "approve own as update-all" },
            Json.GetJsonSerializerOptions());

        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
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
}
