using System.Security.Claims;
using FluentAssertions;
using LumenForgeServer.Auth.Domain;
using LumenForgeServer.Auth.Persistence;
using LumenForgeServer.Common.Exceptions;
using LumenForgeServer.Rentals.Controller;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Dto.Query;
using LumenForgeServer.Rentals.Dto.View;
using LumenForgeServer.Rentals.Persistence;
using LumenForgeServer.Rentals.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NodaTime;
using NSubstitute;

namespace LumenForgeServer.UnitTests.Rentals.Controller;

/// <summary>
/// Unit tests for <see cref="RentalOverviewController"/> — verifies
/// DTO shaping, scoped access handling, and error behavior with mocked dependencies.
/// </summary>
public class RentalOverviewControllerTests
{
    private readonly IRentalProcessRepository _repo = Substitute.For<IRentalProcessRepository>();
    private readonly IAuthRepository _authRepo = Substitute.For<IAuthRepository>();
    private readonly CancellationToken _ct = CancellationToken.None;

    private RentalOverviewController CreateController(
        Permissions[]? permissions = null,
        string? callerKcId = "caller-kc-id",
        IReadOnlyCollection<Guid>? groupGuids = null)
    {
        var claims = new List<Claim>();
        if (!string.IsNullOrWhiteSpace(callerKcId))
        {
            claims.Add(new Claim("sub", callerKcId));
            claims.Add(new Claim(ClaimTypes.NameIdentifier, callerKcId));
        }

        if (permissions is not null)
        {
            claims.AddRange(permissions.Select(p => new Claim(ClaimTypes.Role, p.ToString())));
        }

        _authRepo.GetGroupGuidsForUserAsync(Arg.Any<string>(), _ct)
            .Returns((groupGuids ?? []).ToHashSet());

        var controller = new RentalOverviewController(
            new RentalOverViewService(_repo),
            new RentalAccessService(_authRepo));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
            }
        };

        return controller;
    }

    private static readonly Instant Now = SystemClock.Instance.GetCurrentInstant();

    [Fact]
    public async Task ListProcesses_ReturnsOkWithListAndTotal()
    {
        var process = CreateProcess();
        _repo.ListAsync(Arg.Any<RentalListQueryDto>(), Arg.Any<RentalAccessFilter>(), _ct)
            .Returns(([process], 1));

        var controller = CreateController([Permissions.RentalReadAll]);
        var result = await controller.ListProcesses(new RentalListQueryDto(), _ct);

        result.Should().BeOfType<OkObjectResult>();
        await _repo.Received(1).ListAsync(
            Arg.Any<RentalListQueryDto>(),
            Arg.Is<RentalAccessFilter>(scope => scope.AllowAll),
            _ct);
    }

    [Fact]
    public async Task ListProcesses_WithoutReadScope_ReturnsForbid()
    {
        var controller = CreateController([]);

        var result = await controller.ListProcesses(new RentalListQueryDto(), _ct);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task ListProcesses_WithOwnScope_ScopesToCallerKcId()
    {
        _repo.ListAsync(Arg.Any<RentalListQueryDto>(), Arg.Any<RentalAccessFilter>(), _ct)
            .Returns((new List<RentalProcessInstance>(), 0));

        var controller = CreateController([Permissions.RentalUserOwn], callerKcId: "caller-kc-id");
        await controller.ListProcesses(new RentalListQueryDto(), _ct);

        await _repo.Received(1).ListAsync(
            Arg.Any<RentalListQueryDto>(),
            Arg.Is<RentalAccessFilter>(scope =>
                !scope.AllowAll &&
                scope.OwnerKcId == "caller-kc-id" &&
                scope.GroupGuids.Count == 0),
            _ct);
    }

    [Fact]
    public async Task GetProcess_Found_ReturnsOk()
    {
        var process = CreateProcess();
        _repo.GetByGuidWithIncludesScopedAsync(
                process.Guid,
                RentalProcessInclude.None,
                Arg.Any<RentalAccessFilter>(),
                _ct)
            .Returns(process);

        var controller = CreateController([Permissions.RentalReadAll]);
        var result = await controller.GetProcess(process.Guid, null, _ct);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetProcess_NotFound_ThrowsNotFoundException()
    {
        var guid = Guid.NewGuid();
        _repo.GetByGuidWithIncludesScopedAsync(
                guid,
                RentalProcessInclude.None,
                Arg.Any<RentalAccessFilter>(),
                _ct)
            .Returns((RentalProcessInstance?)null);

        var controller = CreateController([Permissions.RentalReadAll]);
        var act = () => controller.GetProcess(guid, null, _ct);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetProcess_WithIncludes_UsesScopedIncludesQuery()
    {
        var process = CreateProcess();
        _repo.GetByGuidWithIncludesScopedAsync(
                process.Guid,
                Arg.Any<RentalProcessInclude>(),
                Arg.Any<RentalAccessFilter>(),
                _ct)
            .Returns(process);

        var controller = CreateController([Permissions.RentalReadAll]);
        await controller.GetProcess(process.Guid, "checklists", _ct);

        await _repo.Received(1).GetByGuidWithIncludesScopedAsync(
            process.Guid,
            RentalProcessInclude.Checklists,
            Arg.Any<RentalAccessFilter>(),
            _ct);
    }

    [Fact]
    public async Task GetProcess_WithoutReadScope_ReturnsForbid()
    {
        var controller = CreateController([]);

        var result = await controller.GetProcess(Guid.NewGuid(), null, _ct);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task GetProcessHistory_Found_ReturnsOk()
    {
        var process = CreateProcess();
        _repo.GetByGuidWithIncludesScopedAsync(
                process.Guid,
                RentalProcessInclude.None,
                Arg.Any<RentalAccessFilter>(),
                _ct)
            .Returns(process);
        _repo.GetActionLogsByProcessGuidAsync(process.Guid, 50, 0, _ct)
            .Returns((new List<RentalActionLog>(), 0));

        var controller = CreateController([Permissions.RentalReadAll]);
        var result = await controller.GetProcessHistory(process.Guid, ct: _ct);

        result.Should().BeOfType<OkObjectResult>();
    }

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

        _repo.CountByStageAsync(Arg.Any<RentalAccessFilter>(), _ct).Returns(byStage);
        _repo.CountDamageReportsAsync(Arg.Any<RentalAccessFilter>(), _ct).Returns(7);
        _repo.CountExtensionsAsync(Arg.Any<RentalAccessFilter>(), _ct).Returns(4);
        _repo.CountPendingExtensionsAsync(Arg.Any<RentalAccessFilter>(), _ct).Returns(2);
        _repo.CountActionLogsAsync(Arg.Any<RentalAccessFilter>(), _ct).Returns(50);

        var controller = CreateController([Permissions.RentalReadAll]);
        var result = await controller.GetOverview(_ct);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var overview = ok.Value.Should().BeOfType<RentalOverviewDto>().Subject;

        overview.TotalProcesses.Should().Be(21);
        overview.ActiveCount.Should().Be(5);
        overview.TerminalCount.Should().Be(16);
        overview.TotalDamageReports.Should().Be(7);
        overview.TotalExtensionRequests.Should().Be(4);
        overview.PendingExtensions.Should().Be(2);
        overview.TotalActionLogs.Should().Be(50);
    }

    [Fact]
    public async Task GetRecentActivity_ValidDays_ReturnsOk()
    {
        _repo.CountProcessesCreatedSinceAsync(Arg.Any<Instant>(), Arg.Any<RentalAccessFilter>(), _ct).Returns(5);
        _repo.CountProcessesReachedStageSinceAsync(
            RentalStage.Completed,
            Arg.Any<Instant>(),
            Arg.Any<RentalAccessFilter>(),
            _ct).Returns(2);
        _repo.CountProcessesReachedStageSinceAsync(
            RentalStage.Cancelled,
            Arg.Any<Instant>(),
            Arg.Any<RentalAccessFilter>(),
            _ct).Returns(1);
        _repo.CountActionLogsSinceAsync(Arg.Any<Instant>(), Arg.Any<RentalAccessFilter>(), _ct).Returns(20);
        _repo.CountDamageReportsSinceAsync(Arg.Any<Instant>(), Arg.Any<RentalAccessFilter>(), _ct).Returns(3);

        var controller = CreateController([Permissions.RentalReadAll]);
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

    [Fact]
    public async Task GetByStage_ReturnsAllStagesIncludingZeroCounts()
    {
        var byStage = new Dictionary<RentalStage, int> { [RentalStage.Requested] = 3 };
        _repo.CountByStageAsync(Arg.Any<RentalAccessFilter>(), _ct).Returns(byStage);

        var controller = CreateController([Permissions.RentalReadAll]);
        var result = await controller.GetByStage(_ct);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var buckets = ok.Value.Should().BeAssignableTo<List<StageBucketDto>>().Subject;

        var allStages = Enum.GetValues<RentalStage>();
        buckets.Should().HaveCount(allStages.Length);
        buckets.Single(b => b.Stage == RentalStage.Requested).Count.Should().Be(3);
        buckets.Where(b => b.Stage != RentalStage.Requested).Should().OnlyContain(b => b.Count == 0);
    }

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
