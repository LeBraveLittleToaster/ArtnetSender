using FluentAssertions;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Service.Actions;
using NodaTime;

namespace LumenForgeServer.UnitTests.Rentals.Domain;

/// <summary>
/// Tests basic domain entity construction and collection defaults
/// for <see cref="RentalProcessInstance"/>.
/// </summary>
public class RentalProcessEntityTests
{
    [Fact]
    public void NewProcess_DefaultsToStageNone()
    {
        var process = new RentalProcessInstance
        {
            CreatedByKcId = "test-kc"
        };

        process.CurrentStage.Should().Be(RentalStage.None);
    }

    [Fact]
    public void NewProcess_CollectionsAreEmpty()
    {
        var process = new RentalProcessInstance
        {
            CreatedByKcId = "test-kc"
        };

        process.ActionLogs.Should().BeEmpty();
        process.Checklists.Should().BeEmpty();
        process.Extensions.Should().BeEmpty();
        process.DamageReports.Should().BeEmpty();
    }

    [Fact]
    public void NewProcess_RentalIsNull()
    {
        var process = new RentalProcessInstance
        {
            CreatedByKcId = "test-kc"
        };

        process.Rental.Should().BeNull();
        process.RentalId.Should().BeNull();
    }

    [Fact]
    public void SetCurrentStage_UpdatesStage()
    {
        var process = new RentalProcessInstance
        {
            CreatedByKcId = "test-kc",
            CurrentStage = RentalStage.None
        };

        process.CurrentStage = RentalStage.Requested;

        process.CurrentStage.Should().Be(RentalStage.Requested);
    }

    [Fact]
    public void ActionLogs_CanBeAppended()
    {
        var process = new RentalProcessInstance
        {
            CreatedByKcId = "test-kc"
        };

        var log = new RentalActionLog
        {
            Guid = Guid.NewGuid(),
            ProcessInstanceId = process.Id,
            ActionType = RentalActionType.CreateRental,
            PerformedByKcId = "actor-kc",
            StageBefore = RentalStage.None,
            StageAfter = RentalStage.Requested,
            Success = true,
            PerformedAt = SystemClock.Instance.GetCurrentInstant()
        };

        process.ActionLogs.Add(log);

        process.ActionLogs.Should().ContainSingle();
        process.ActionLogs[0].ActionType.Should().Be(RentalActionType.CreateRental);
    }

    [Fact]
    public void LinkRental_SetsBidirectionalNavigation()
    {
        var process = new RentalProcessInstance
        {
            Id = 42,
            CreatedByKcId = "test-kc"
        };

        var rental = new Rental
        {
            Id = 42,
            Uuid = Guid.NewGuid(),
            CustomerKcId = "customer-kc",
            RequestedStart = SystemClock.Instance.GetCurrentInstant(),
            RequestedEnd = SystemClock.Instance.GetCurrentInstant() + Duration.FromDays(7),
            CreatedAt = SystemClock.Instance.GetCurrentInstant(),
            UpdatedAt = SystemClock.Instance.GetCurrentInstant()
        };

        process.RentalId = rental.Id;
        process.Rental = rental;

        process.Rental.Should().BeSameAs(rental);
        process.RentalId.Should().Be(42);
    }
}
