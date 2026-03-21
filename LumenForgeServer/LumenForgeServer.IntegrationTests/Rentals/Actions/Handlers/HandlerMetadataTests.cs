using FluentAssertions;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Persistence;
using LumenForgeServer.IntegrationTests.Rentals.Actions.Helpers;
using LumenForgeServer.Rentals.Service.Actions;
using LumenForgeServer.Rentals.Service.Actions.Handlers;
using NSubstitute;

namespace LumenForgeServer.IntegrationTests.Rentals.Actions.Handlers;

/// <summary>
/// Verifies that every handler declares the correct <see cref="RentalActionType"/>
/// and <see cref="IRentalActionHandler.AllowedStages"/> set.
/// </summary>
public class HandlerMetadataTests
{
    // ── Factory helpers (creates handlers with substitute/null deps) ─────

    private static IRentalActionHandler Handler<T>(T handler) where T : IRentalActionHandler => handler;

    private static readonly IRentalProcessRepository Repo = Substitute.For<IRentalProcessRepository>();
    private static readonly global::LumenForgeServer.Common.Database.AppDbContext Db = HandlerTestHelper.CreateInMemoryDbContext();
    private static readonly global::LumenForgeServer.Inventory.Service.StockBindingService StockSvc = HandlerTestHelper.CreateStockBindingService();

    // ── ActionType ──────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(ActionTypeData))]
    public void Handler_ActionType_Matches(IRentalActionHandler handler, RentalActionType expected)
    {
        handler.ActionType.Should().Be(expected);
    }

    public static TheoryData<IRentalActionHandler, RentalActionType> ActionTypeData() => new()
    {
        { new CreateRentalHandler(Repo), RentalActionType.CreateRental },
        { new ApproveRequestHandler(), RentalActionType.ApproveRequest },
        { new RejectRequestHandler(), RentalActionType.RejectRequest },
        { new AssignItemsHandler(StockSvc), RentalActionType.AssignItems },
        { new RemoveItemsHandler(StockSvc), RentalActionType.RemoveItems },
        { new ApproveItemsHandler(), RentalActionType.ApproveItems },
        { new RejectItemsHandler(), RentalActionType.RejectItems },
        { new GenerateChecklistHandler(Repo, Db), RentalActionType.GenerateChecklist },
        { new ScanChecklistHandler(Repo), RentalActionType.ScanChecklist },
        { new SignChecklistHandler(Repo), RentalActionType.SignChecklist },
        { new RecordPickupHandler(), RentalActionType.RecordPickup },
        { new RecordReturnHandler(), RentalActionType.RecordReturn },
        { new RequestExtensionHandler(Repo), RentalActionType.RequestExtension },
        { new ApproveExtensionHandler(Repo), RentalActionType.ApproveExtension },
        { new RejectExtensionHandler(Repo), RentalActionType.RejectExtension },
        { new RecordDamagesHandler(Repo), RentalActionType.RecordDamages },
        { new CreateMaintenanceJobsHandler(Repo, Db), RentalActionType.CreateMaintenanceJobs },
        { new GenerateInvoiceHandler(Db), RentalActionType.GenerateInvoice },
        { new RecordPaymentHandler(Db), RentalActionType.RecordPayment },
        { new GenerateReportHandler(Repo), RentalActionType.GenerateReport },
        { new CompleteRentalHandler(), RentalActionType.CompleteRental },
        { new CancelRentalHandler(), RentalActionType.CancelRental },
        { new ScrapRentalHandler(), RentalActionType.ScrapRental },
    };

    // ── AllowedStages ───────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(AllowedStagesData))]
    public void Handler_AllowedStages_AreCorrect(
        IRentalActionHandler handler, HashSet<RentalStage> expectedStages)
    {
        handler.AllowedStages.Should().BeEquivalentTo(expectedStages);
    }

    public static TheoryData<IRentalActionHandler, HashSet<RentalStage>> AllowedStagesData() => new()
    {
        { new CreateRentalHandler(Repo), new HashSet<RentalStage> { RentalStage.None } },
        { new ApproveRequestHandler(), new HashSet<RentalStage> { RentalStage.Requested } },
        { new RejectRequestHandler(), new HashSet<RentalStage> { RentalStage.Requested } },
        { new AssignItemsHandler(StockSvc), new HashSet<RentalStage> { RentalStage.Approved, RentalStage.ItemsAssigned } },
        { new RemoveItemsHandler(StockSvc), new HashSet<RentalStage> { RentalStage.ItemsAssigned } },
        { new ApproveItemsHandler(), new HashSet<RentalStage> { RentalStage.ItemsAssigned } },
        { new RejectItemsHandler(), new HashSet<RentalStage> { RentalStage.ItemsAssigned } },
        { new GenerateChecklistHandler(Repo, Db), new HashSet<RentalStage> { RentalStage.ItemsApproved } },
        { new ScanChecklistHandler(Repo), new HashSet<RentalStage> { RentalStage.ReadyForPickup } },
        { new SignChecklistHandler(Repo), new HashSet<RentalStage> { RentalStage.ReadyForPickup } },
        { new RecordPickupHandler(), new HashSet<RentalStage> { RentalStage.ReadyForPickup } },
        { new RecordReturnHandler(), new HashSet<RentalStage> { RentalStage.PickedUp } },
        { new RequestExtensionHandler(Repo), new HashSet<RentalStage> { RentalStage.PickedUp } },
        { new ApproveExtensionHandler(Repo), new HashSet<RentalStage> { RentalStage.PickedUp } },
        { new RejectExtensionHandler(Repo), new HashSet<RentalStage> { RentalStage.PickedUp } },
        { new RecordDamagesHandler(Repo), new HashSet<RentalStage> { RentalStage.Returned } },
        { new CreateMaintenanceJobsHandler(Repo, Db), new HashSet<RentalStage> { RentalStage.Returned, RentalStage.Inspected } },
        { new GenerateInvoiceHandler(Db), new HashSet<RentalStage> { RentalStage.Returned, RentalStage.Inspected } },
        { new RecordPaymentHandler(Db), new HashSet<RentalStage> { RentalStage.Invoiced } },
        { new GenerateReportHandler(Repo), new HashSet<RentalStage> { RentalStage.Paid, RentalStage.Completed, RentalStage.Scrapped } },
        { new CompleteRentalHandler(), new HashSet<RentalStage> { RentalStage.Paid } },
        { new CancelRentalHandler(), new HashSet<RentalStage> { RentalStage.Requested, RentalStage.Approved, RentalStage.ItemsAssigned, RentalStage.ItemsApproved, RentalStage.ReadyForPickup } },
        { new ScrapRentalHandler(), new HashSet<RentalStage> { RentalStage.PickedUp } },
    };
}
