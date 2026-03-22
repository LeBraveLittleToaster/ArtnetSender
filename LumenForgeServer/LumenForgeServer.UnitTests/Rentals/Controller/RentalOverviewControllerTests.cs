using FluentAssertions;
using LumenForgeServer.Auth.Domain.Session;
using LumenForgeServer.Common.Exceptions;
using LumenForgeServer.Rentals.Controller;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Dto.Query;
using LumenForgeServer.Rentals.Dto.View;
using LumenForgeServer.Rentals.Persistence;
using Microsoft.AspNetCore.Mvc;
using NodaTime;
using NSubstitute;

namespace LumenForgeServer.UnitTests.Rentals.Controller;

/// <summary>
/// Unit tests for <see cref="RentalOverviewController"/> — verifies
/// correct DTO shaping, scoping, and error handling with a mocked repository.
/// </summary>
public class RentalOverviewControllerTests
{
    private readonly IRentalProcessRepository _repo = Substitute.For<IRentalProcessRepository>();
    private readonly IKeycloakUser _keycloakUser = Substitute.For<IKeycloakUser>();
    private readonly CancellationToken _ct = CancellationToken.None;

    private RentalOverviewController CreateController()
        => new(_repo, _keycloakUser);

    private static readonly Instant Now = SystemClock.Instance.GetCurrentInstant();

    // ── ListProcesses ───────────────────────────────────────────────

    [Fact]
    public async Task ListProcesses_ReturnsOkWithListAndTotal()
    {
        var process = CreateProcess();
        _repo.ListAsync(Arg.Any<RentalListQueryDto>(), _ct)
            .Returns(([process], 1));

        var controller = CreateController();
        var result = await controller.ListProcesses(new RentalListQueryDto(), _ct);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ListProcesses_EmptyList_ReturnsOkWithZeroTotal()
    {
        _repo.ListAsync(Arg.Any<RentalListQueryDto>(), _ct)
            .Returns((new List<RentalProcessInstance>(), 0));

        var controller = CreateController();
        var result = await controller.ListProcesses(new RentalListQueryDto(), _ct);

        result.Should().BeOfType<OkObjectResult>();
    }

    // ── ListMyProcesses ─────────────────────────────────────────────

    [Fact]
    public async Task ListMyProcesses_ScopesToCallerKcId()
    {
        _keycloakUser.UserId.Returns("caller-kc-id");
        _repo.ListAsync(Arg.Any<RentalListQueryDto>(), _ct)
            .Returns((new List<RentalProcessInstance>(), 0));

        var controller = CreateController();
        await controller.ListMyProcesses(new RentalListQueryDto(), _ct);

        await _repo.Received(1).ListAsync(
            Arg.Is<RentalListQueryDto>(q => q.OwnerKcId == "caller-kc-id"),
            _ct);
    }

    [Fact]
    public async Task ListMyProcesses_NoUserId_ThrowsUnauthorized()
    {
        _keycloakUser.UserId.Returns((string?)null);

        var controller = CreateController();

        var act = () => controller.ListMyProcesses(new RentalListQueryDto(), _ct);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    // ── GetProcess ──────────────────────────────────────────────────

    [Fact]
    public async Task GetProcess_Found_ReturnsOk()
    {
        var process = CreateProcess();
        _repo.GetByGuidAsync(process.Guid, _ct).Returns(process);

        var controller = CreateController();
        var result = await controller.GetProcess(process.Guid, null, _ct);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetProcess_NotFound_ThrowsNotFoundException()
    {
        var guid = Guid.NewGuid();
        _repo.GetByGuidAsync(guid, _ct).Returns((RentalProcessInstance?)null);

        var controller = CreateController();

        var act = () => controller.GetProcess(guid, null, _ct);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetProcess_WithIncludes_UsesGetByGuidWithIncludesAsync()
    {
        var process = CreateProcess();
        _repo.GetByGuidWithIncludesAsync(process.Guid, Arg.Any<RentalProcessInclude>(), _ct)
            .Returns(process);

        var controller = CreateController();
        await controller.GetProcess(process.Guid, "checklists", _ct);

        await _repo.Received(1).GetByGuidWithIncludesAsync(
            process.Guid,
            RentalProcessInclude.Checklists,
            _ct);
    }

    [Fact]
    public async Task GetProcess_WithoutIncludes_UsesGetByGuidAsync()
    {
        var process = CreateProcess();
        _repo.GetByGuidAsync(process.Guid, _ct).Returns(process);

        var controller = CreateController();
        await controller.GetProcess(process.Guid, null, _ct);

        await _repo.Received(1).GetByGuidAsync(process.Guid, _ct);
        await _repo.DidNotReceive().GetByGuidWithIncludesAsync(
            Arg.Any<Guid>(), Arg.Any<RentalProcessInclude>(), _ct);
    }

    // ── GetProcessHistory ───────────────────────────────────────────

    [Fact]
    public async Task GetProcessHistory_Found_ReturnsOk()
    {
        var process = CreateProcess();
        _repo.GetByGuidAsync(process.Guid, _ct).Returns(process);
        _repo.GetActionLogsByProcessGuidAsync(process.Guid, 50, 0, _ct)
            .Returns((new List<RentalActionLog>(), 0));

        var controller = CreateController();
        var result = await controller.GetProcessHistory(process.Guid, ct: _ct);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetProcessHistory_ProcessNotFound_ThrowsNotFoundException()
    {
        var guid = Guid.NewGuid();
        _repo.GetByGuidAsync(guid, _ct).Returns((RentalProcessInstance?)null);

        var controller = CreateController();

        var act = () => controller.GetProcessHistory(guid, ct: _ct);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── GetOverview ─────────────────────────────────────────────────

    [Fact]
    public async Task GetOverview_ComputesActiveAndTerminalCounts()
    {
        var byStage = new Dictionary<RentalStage, int>
        {
            [RentalStage.Requested] = 3,
            [RentalStage.Approved] = 2,
            [RentalStage.Completed] = 10,
            [RentalStage.Cancelled] = 5,
            [RentalStage.Scrapped] = 1
        };

        _repo.CountByStageAsync(_ct).Returns(byStage);
        _repo.CountDamageReportsAsync(_ct).Returns(7);
        _repo.CountExtensionsAsync(_ct).Returns(4);
        _repo.CountPendingExtensionsAsync(_ct).Returns(2);
        _repo.CountActionLogsAsync(_ct).Returns(50);

        var controller = CreateController();
        var result = await controller.GetOverview(_ct);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var overview = ok.Value.Should().BeOfType<RentalOverviewDto>().Subject;

        overview.TotalProcesses.Should().Be(21);
        overview.ActiveCount.Should().Be(5);    // 3 + 2
        overview.TerminalCount.Should().Be(16);  // 10 + 5 + 1
        overview.TotalDamageReports.Should().Be(7);
        overview.TotalExtensionRequests.Should().Be(4);
        overview.PendingExtensions.Should().Be(2);
        overview.TotalActionLogs.Should().Be(50);
    }

    // ── GetRecentActivity ───────────────────────────────────────────

    [Fact]
    public async Task GetRecentActivity_InvalidDays_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = await controller.GetRecentActivity(days: 0, ct: _ct);
        result.Should().BeOfType<BadRequestObjectResult>();

        result = await controller.GetRecentActivity(days: 400, ct: _ct);
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetRecentActivity_ValidDays_ReturnsOk()
    {
        _repo.CountProcessesCreatedSinceAsync(Arg.Any<Instant>(), _ct).Returns(5);
        _repo.CountProcessesReachedStageSinceAsync(RentalStage.Completed, Arg.Any<Instant>(), _ct).Returns(2);
        _repo.CountProcessesReachedStageSinceAsync(RentalStage.Cancelled, Arg.Any<Instant>(), _ct).Returns(1);
        _repo.CountActionLogsSinceAsync(Arg.Any<Instant>(), _ct).Returns(20);
        _repo.CountDamageReportsSinceAsync(Arg.Any<Instant>(), _ct).Returns(3);

        var controller = CreateController();
        var result = await controller.GetRecentActivity(days: 14, ct: _ct);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var activity = ok.Value.Should().BeOfType<RentalRecentActivityDto>().Subject;

        activity.ProcessesCreated.Should().Be(5);
        activity.ProcessesCompleted.Should().Be(2);
        activity.ProcessesCancelled.Should().Be(1);
        activity.ActionsPerformed.Should().Be(20);
        activity.DamagesReported.Should().Be(3);
        activity.WindowDays.Should().Be(14);
    }

    // ── GetByStage ──────────────────────────────────────────────────

    [Fact]
    public async Task GetByStage_ReturnsAllStagesIncludingZeroCounts()
    {
        var byStage = new Dictionary<RentalStage, int>
        {
            [RentalStage.Requested] = 3
        };
        _repo.CountByStageAsync(_ct).Returns(byStage);

        var controller = CreateController();
        var result = await controller.GetByStage(_ct);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var buckets = ok.Value.Should().BeAssignableTo<List<StageBucketDto>>().Subject;

        var allStages = Enum.GetValues<RentalStage>();
        buckets.Should().HaveCount(allStages.Length);
        buckets.Single(b => b.Stage == RentalStage.Requested).Count.Should().Be(3);
        buckets.Where(b => b.Stage != RentalStage.Requested).Should().OnlyContain(b => b.Count == 0);
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private static RentalProcessInstance CreateProcess()
    {
        return new RentalProcessInstance
        {
            Id = 1,
            Guid = Guid.NewGuid(),
            CurrentStage = RentalStage.Approved,
            CreatedByKcId = "test-kc-id",
            CreatedAt = Now,
            UpdatedAt = Now,
            RentalId = 1,
            Rental = new Rental
            {
                Id = 1,
                Uuid = Guid.NewGuid(),
                CustomerKcId = "customer-kc-id",
                CustomerName = "Test Customer",
                CustomerEmail = "test@example.com",
                Purpose = "Testing",
                RequestedStart = Now,
                RequestedEnd = Now + Duration.FromDays(7),
                CreatedAt = Now,
                UpdatedAt = Now
            }
        };
    }
}
