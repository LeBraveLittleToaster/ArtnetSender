using FluentAssertions;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Service.Actions;
using LumenForgeServer.Rentals.Service.Actions.Handlers;
using LumenForgeServer.UnitTests.Rentals.Actions.Helpers;
using NSubstitute;

namespace LumenForgeServer.UnitTests.Rentals.Domain;

/// <summary>
/// Verifies that the handler-declared <c>AllowedStages</c> are consistent
/// with the <see cref="RentalActionRegistry"/> lookup table and that
/// every stage transition in the workflow forms a valid directed graph.
/// </summary>
public class RentalStageTransitionTests
{
    private static readonly RentalActionRegistry Registry = new();

    [Fact]
    public void AllHandlerAllowedStages_AreRegisteredInRegistry()
    {
        var repo = Substitute.For<LumenForgeServer.Rentals.Persistence.IRentalProcessRepository>();
        var questionRepo = Substitute.For<LumenForgeServer.Rentals.Persistence.IQuestionRepository>();
        var db = HandlerTestHelper.CreateInMemoryDbContext();
        var stockSvc = HandlerTestHelper.CreateStockBindingService();

        IRentalActionHandler[] handlers =
        [
            new CreateRentalHandler(repo, questionRepo),
            new ApproveRequestHandler(),
            new RejectRequestHandler(),
            new AssignItemsHandler(stockSvc),
            new RemoveItemsHandler(stockSvc),
            new ApproveItemsHandler(),
            new RejectItemsHandler(),
            new GenerateChecklistHandler(repo, db),
            new ScanChecklistHandler(repo),
            new SignChecklistHandler(repo),
            new RecordPickupHandler(),
            new RecordReturnHandler(),
            new RequestExtensionHandler(repo),
            new ApproveExtensionHandler(repo),
            new RejectExtensionHandler(repo),
            new RecordDamagesHandler(repo),
            new CreateMaintenanceJobsHandler(repo, db),
            new GenerateInvoiceHandler(db),
            new RecordPaymentHandler(db),
            new GenerateReportHandler(repo),
            new CompleteRentalHandler(),
            new CancelRentalHandler(),
            new ScrapRentalHandler()
        ];

        foreach (var handler in handlers)
        {
            foreach (var stage in handler.AllowedStages)
            {
                var registryActions = Registry.GetAvailableActions(stage);
                registryActions.Should().Contain(handler.ActionType,
                    because: $"handler {handler.GetType().Name} declares AllowedStage={stage} " +
                             $"but the registry does not list {handler.ActionType} for that stage");
            }
        }
    }

    [Fact]
    public void EveryRegistryAction_HasACorrespondingHandlerActionType()
    {
        var allStages = Enum.GetValues<RentalStage>();
        var allRegistryActions = allStages
            .SelectMany(s => Registry.GetAvailableActions(s))
            .ToHashSet();

        var allHandlerActionTypes = Enum.GetValues<RentalActionType>();

        allRegistryActions.Should().BeEquivalentTo(allHandlerActionTypes,
            because: "the registry should cover all defined action types");
    }

    [Fact]
    public void HappyPathSequence_IsNavigable()
    {
        // The "golden path" from creation to completion
        var happyPath = new[]
        {
            (RentalStage.None, RentalActionType.CreateRental),
            (RentalStage.Requested, RentalActionType.ApproveRequest),
            (RentalStage.Approved, RentalActionType.AssignItems),
            (RentalStage.ItemsAssigned, RentalActionType.ApproveItems),
            (RentalStage.ItemsApproved, RentalActionType.GenerateChecklist),
            (RentalStage.ReadyForPickup, RentalActionType.RecordPickup),
            (RentalStage.PickedUp, RentalActionType.RecordReturn),
            (RentalStage.Returned, RentalActionType.GenerateInvoice),
            (RentalStage.Invoiced, RentalActionType.RecordPayment),
            (RentalStage.Paid, RentalActionType.CompleteRental),
        };

        foreach (var (stage, action) in happyPath)
        {
            var available = Registry.GetAvailableActions(stage);
            available.Should().Contain(action,
                because: $"the happy path expects {action} to be available at stage {stage}");
        }
    }

    [Theory]
    [InlineData(RentalStage.Requested)]
    [InlineData(RentalStage.Approved)]
    [InlineData(RentalStage.ItemsAssigned)]
    [InlineData(RentalStage.ItemsApproved)]
    [InlineData(RentalStage.ReadyForPickup)]
    public void CancelRental_AvailableInAllPrePickupStages(RentalStage stage)
    {
        Registry.GetAvailableActions(stage)
            .Should().Contain(RentalActionType.CancelRental);
    }

    [Theory]
    [InlineData(RentalStage.PickedUp)]
    [InlineData(RentalStage.Returned)]
    [InlineData(RentalStage.Inspected)]
    [InlineData(RentalStage.Invoiced)]
    [InlineData(RentalStage.Paid)]
    public void CancelRental_NotAvailablePostPickup(RentalStage stage)
    {
        Registry.GetAvailableActions(stage)
            .Should().NotContain(RentalActionType.CancelRental);
    }
}
