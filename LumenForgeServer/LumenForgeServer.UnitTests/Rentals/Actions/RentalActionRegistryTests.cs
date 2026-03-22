using FluentAssertions;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Service.Actions;

namespace LumenForgeServer.UnitTests.Rentals.Actions;

/// <summary>
/// Verifies the <see cref="RentalActionRegistry"/> lookup table is consistent:
/// every non-terminal stage has at least one action, terminal stages have none,
/// and the registry matches the expected handler AllowedStages.
/// </summary>
public class RentalActionRegistryTests
{
    private readonly RentalActionRegistry _registry = new();

    [Theory]
    [InlineData(RentalStage.None)]
    [InlineData(RentalStage.Requested)]
    [InlineData(RentalStage.Approved)]
    [InlineData(RentalStage.ItemsAssigned)]
    [InlineData(RentalStage.ItemsApproved)]
    [InlineData(RentalStage.ReadyForPickup)]
    [InlineData(RentalStage.PickedUp)]
    [InlineData(RentalStage.Returned)]
    [InlineData(RentalStage.Inspected)]
    [InlineData(RentalStage.Invoiced)]
    [InlineData(RentalStage.Paid)]
    public void NonTerminalStage_HasAtLeastOneAction(RentalStage stage)
    {
        var actions = _registry.GetAvailableActions(stage);

        actions.Should().NotBeEmpty(
            because: $"stage {stage} should have available actions");
    }

    [Fact]
    public void CancelledStage_HasNoActions()
    {
        var actions = _registry.GetAvailableActions(RentalStage.Cancelled);

        actions.Should().BeEmpty(
            because: "Cancelled is a fully terminal stage with no further actions");
    }

    [Theory]
    [InlineData(RentalStage.Completed)]
    [InlineData(RentalStage.Scrapped)]
    public void TerminalStage_OnlyAllowsGenerateReport(RentalStage stage)
    {
        var actions = _registry.GetAvailableActions(stage);

        actions.Should().ContainSingle()
            .Which.Should().Be(RentalActionType.GenerateReport,
                because: $"terminal stage {stage} should only allow report generation");
    }

    [Fact]
    public void RequestedStage_ContainsExpectedActions()
    {
        var actions = _registry.GetAvailableActions(RentalStage.Requested);

        actions.Should().Contain(RentalActionType.ApproveRequest);
        actions.Should().Contain(RentalActionType.RejectRequest);
        actions.Should().Contain(RentalActionType.CancelRental);
    }

    [Fact]
    public void NoneStage_OnlyContainsCreateRental()
    {
        var actions = _registry.GetAvailableActions(RentalStage.None);

        actions.Should().ContainSingle()
            .Which.Should().Be(RentalActionType.CreateRental);
    }

    [Fact]
    public void PickedUpStage_IncludesExtensionAndScrap()
    {
        var actions = _registry.GetAvailableActions(RentalStage.PickedUp);

        actions.Should().Contain(RentalActionType.RecordReturn);
        actions.Should().Contain(RentalActionType.RequestExtension);
        actions.Should().Contain(RentalActionType.ApproveExtension);
        actions.Should().Contain(RentalActionType.RejectExtension);
        actions.Should().Contain(RentalActionType.ScrapRental);
    }

    [Fact]
    public void AllRentalActionTypes_AreCoveredByAtLeastOneStage()
    {
        var allActions = Enum.GetValues<RentalActionType>();
        var allStages = Enum.GetValues<RentalStage>();

        var coveredActions = allStages
            .SelectMany(s => _registry.GetAvailableActions(s))
            .ToHashSet();

        coveredActions.Should().BeEquivalentTo(allActions,
            because: "every action type should be reachable from at least one stage");
    }

    [Fact]
    public void GenerateReport_AvailableInPaidCompletedAndScrapped()
    {
        _registry.GetAvailableActions(RentalStage.Paid)
            .Should().Contain(RentalActionType.GenerateReport);

        _registry.GetAvailableActions(RentalStage.Completed)
            .Should().Contain(RentalActionType.GenerateReport);

        _registry.GetAvailableActions(RentalStage.Scrapped)
            .Should().Contain(RentalActionType.GenerateReport);
    }
}
