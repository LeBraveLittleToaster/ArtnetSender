using FluentAssertions;
using LumenForgeServer.Auth.Domain;
using LumenForgeServer.Common.Exceptions;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Dto.Command;
using LumenForgeServer.Rentals.Persistence;
using LumenForgeServer.UnitTests.Rentals.Actions.Helpers;
using LumenForgeServer.Rentals.Service;
using LumenForgeServer.Rentals.Service.Actions;
using LumenForgeServer.Rentals.Service.Actions.Handlers;
using Microsoft.Extensions.Logging;
using NodaTime;
using NSubstitute;

namespace LumenForgeServer.UnitTests.Rentals.Actions;

/// <summary>
/// Tests the <see cref="RentalActionService"/> orchestrator:
/// lifecycle routing, stage gating, stage transitions, logging,
/// and the <see cref="RentalActionService.CreateProcessAsync"/> path.
/// </summary>
public class RentalActionServiceTests
{
    private readonly IRentalProcessRepository _repo = Substitute.For<IRentalProcessRepository>();
    private readonly IQuestionRepository _questionRepo = Substitute.For<IQuestionRepository>();
    private readonly IRentalActionRegistry _registry = new RentalActionRegistry();
    private readonly ILogger<RentalActionService> _logger = Substitute.For<ILogger<RentalActionService>>();
    private readonly CancellationToken _ct = CancellationToken.None;

    private RentalActionService CreateService(params IRentalActionHandler[] handlers)
        => new(handlers, _repo, _registry, _logger);

    // ── ExecuteActionAsync ──────────────────────────────────────────

    [Fact]
    public async Task ExecuteAction_ProcessNotFound_ThrowsNotFoundException()
    {
        var guid = Guid.NewGuid();
        _repo.GetByGuidAsync(guid, _ct).Returns((RentalProcessInstance?)null);

        var service = CreateService(new ApproveRequestHandler());

        var act = () => service.ExecuteActionAsync(
            guid, RentalActionType.ApproveRequest,

            new ApproveRequestInput { ActorKcId = "actor" }, _ct);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ExecuteAction_ActionNotAllowedInStage_ThrowsValidationException()
    {
        var process = HandlerTestHelper.CreateProcess(RentalStage.PickedUp);
        _repo.GetByGuidAsync(process.Guid, _ct).Returns(process);

        // ApproveRequest is only valid in Requested stage
        var service = CreateService(new ApproveRequestHandler());

        var act = () => service.ExecuteActionAsync(
            process.Guid, RentalActionType.ApproveRequest,

            new ApproveRequestInput { ActorKcId = "actor" }, _ct);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task ExecuteAction_NoHandlerRegistered_ThrowsValidationException()
    {
        var process = HandlerTestHelper.CreateProcess(RentalStage.Requested);
        _repo.GetByGuidAsync(process.Guid, _ct).Returns(process);

        // Empty handler list — no handlers registered
        var service = CreateService();

        var act = () => service.ExecuteActionAsync(
            process.Guid, RentalActionType.ApproveRequest,

            new ApproveRequestInput { ActorKcId = "actor" }, _ct);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task ExecuteAction_SuccessfulAction_TransitionsStage()
    {
        var process = HandlerTestHelper.CreateProcess(RentalStage.Requested);
        _repo.GetByGuidAsync(process.Guid, _ct).Returns(process);

        var service = CreateService(new ApproveRequestHandler());

        var result = await service.ExecuteActionAsync(
            process.Guid, RentalActionType.ApproveRequest,

            new ApproveRequestInput { ActorKcId = "actor" }, _ct);

        result.Success.Should().BeTrue();
        process.CurrentStage.Should().Be(RentalStage.Approved);

        await _repo.Received(1).UpdateAsync(process, _ct);
        await _repo.Received(1).AddActionLogAsync(Arg.Any<RentalActionLog>(), _ct);
        await _repo.Received(1).SaveChangesAsync(_ct);
    }

    [Fact]
    public async Task ExecuteAction_FailedBeforeExecute_DoesNotTransitionStage()
    {
        var now = SystemClock.Instance.GetCurrentInstant();
        var process = HandlerTestHelper.CreateProcess(RentalStage.None, withRental: false);
        _repo.GetByGuidAsync(process.Guid, _ct).Returns(process);

        // CreateRental's BeforeExecute validates dates — end before start should fail
        var service = CreateService(new CreateRentalHandler(_repo, _questionRepo));

        var input = new CreateRentalInput
        {
            ActorKcId = "actor",
            RequestedStart = now + Duration.FromDays(5),
            RequestedEnd = now + Duration.FromDays(1) // invalid
        };

        var result = await service.ExecuteActionAsync(
            process.Guid, RentalActionType.CreateRental,  input, _ct);

        result.Success.Should().BeFalse();
        process.CurrentStage.Should().Be(RentalStage.None); // not changed

        // Log should still be written, but Update should not have been called for stage transition
        await _repo.Received(1).AddActionLogAsync(Arg.Any<RentalActionLog>(), _ct);
    }

    [Fact]
    public async Task ExecuteAction_ActionWithNoStageChange_KeepsStage()
    {
        var process = HandlerTestHelper.CreateProcess(RentalStage.ReadyForPickup);
        _repo.GetByGuidAsync(process.Guid, _ct).Returns(process);

        var checklist = HandlerTestHelper.CreateChecklist(process.Id, itemCount: 2);
        var mockRepo = Substitute.For<IRentalProcessRepository>();
        mockRepo.GetChecklistByGuidAsync(checklist.Guid, _ct).Returns(checklist);
        mockRepo.GetByGuidAsync(process.Guid, _ct).Returns(process);

        var handler = new ScanChecklistHandler(mockRepo);
        var service = new RentalActionService(
            [handler], mockRepo, _registry, _logger);

        var input = new ScanChecklistInput
        {
            ActorKcId = "scanner",
            ChecklistGuid = checklist.Guid,
            ScannedValue = "SN-123"
        };

        var result = await service.ExecuteActionAsync(
            process.Guid, RentalActionType.ScanChecklist, input, _ct);

        result.Success.Should().BeTrue();
        result.NewStage.Should().BeNull();
        process.CurrentStage.Should().Be(RentalStage.ReadyForPickup); // unchanged
    }

    // ── CreateProcessAsync ──────────────────────────────────────────

    [Fact]
    public async Task CreateProcess_CreatesInstanceAndRunsHandler()
    {
        var now = SystemClock.Instance.GetCurrentInstant();
        var service = CreateService(new CreateRentalHandler(_repo, _questionRepo));
        var questionGuid = Guid.NewGuid();

        _questionRepo.DoesQuestionExistByGuidAsync(Arg.Any<List<Guid>>()).Returns(0);
        _questionRepo.GetQuestionIdsByGuidAsync(Arg.Any<List<Guid>>()).Returns(new Dictionary<Guid, long>
        {
            [questionGuid] = 200
        });

        var input = new CreateRentalInput
        {
            ActorKcId = "customer",
            CustomerName = "Bob",
            RequestedStart = now + Duration.FromDays(1),
            RequestedEnd = now + Duration.FromDays(5),
            QASets = [new QASet { Guid = questionGuid.ToString(), Value = "Yes" }]
        };

        var result = await service.CreateProcessAsync(input,  _ct);

        result.Success.Should().BeTrue();
        result.NewStage.Should().Be(RentalStage.Requested);
        result.Should().BeOfType<CreateRentalResult>();
        ((CreateRentalResult)result).ProcessInstanceGuid.Should().NotBeEmpty();

        await _repo.Received(1).AddAsync(Arg.Any<RentalProcessInstance>(), _ct);
        await _repo.Received(1).AddRentalAsync(Arg.Any<Rental>(), _ct);
        await _repo.Received(1).SaveChangesAsync(_ct);
    }

}
