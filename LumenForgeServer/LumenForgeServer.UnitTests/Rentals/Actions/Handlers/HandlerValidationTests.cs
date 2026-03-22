using FluentAssertions;
using LumenForgeServer.Common;
using LumenForgeServer.Common.Database;
using LumenForgeServer.Inventory.Service;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Persistence;
using LumenForgeServer.UnitTests.Rentals.Actions.Helpers;
using LumenForgeServer.Rentals.Service.Actions;
using LumenForgeServer.Rentals.Service.Actions.Handlers;
using NodaTime;
using NSubstitute;

namespace LumenForgeServer.UnitTests.Rentals.Actions.Handlers;

/// <summary>
/// Tests the <c>BeforeExecuteAsync</c> validation logic for handlers that
/// override the default (pass-through) implementation.
/// </summary>
public class HandlerValidationTests
{
    private readonly CancellationToken _ct = CancellationToken.None;
    private readonly IRentalProcessRepository _repo = Substitute.For<IRentalProcessRepository>();

    // ── CreateRental ────────────────────────────────────────────────

    [Fact]
    public async Task CreateRental_EndBeforeStart_Fails()
    {
        IRentalActionHandler handler = new CreateRentalHandler(_repo);
        var process = HandlerTestHelper.CreateProcess(RentalStage.None);
        var now = SystemClock.Instance.GetCurrentInstant();

        var input = new CreateRentalInput
        {
            ActorKcId = "actor",
            RequestedStart = now + Duration.FromDays(5),
            RequestedEnd = now + Duration.FromDays(1) // end before start
        };

        var result = await handler.BeforeExecuteAsync(process, input, _ct);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainKey("RequestedEnd");
    }

    [Fact]
    public async Task CreateRental_ValidDates_Passes()
    {
        IRentalActionHandler handler = new CreateRentalHandler(_repo);
        var process = HandlerTestHelper.CreateProcess(RentalStage.None);
        var now = SystemClock.Instance.GetCurrentInstant();

        var input = new CreateRentalInput
        {
            ActorKcId = "actor",
            RequestedStart = now + Duration.FromDays(1),
            RequestedEnd = now + Duration.FromDays(5)
        };

        var result = await handler.BeforeExecuteAsync(process, input, _ct);

        result.Success.Should().BeTrue();
    }

    // ── AssignItems ─────────────────────────────────────────────────

    [Fact]
    public async Task AssignItems_EmptyItems_Fails()
    {
        IRentalActionHandler handler = new AssignItemsHandler(HandlerTestHelper.CreateStockBindingService());
        var process = HandlerTestHelper.CreateProcess(RentalStage.Approved);

        var input = new AssignItemsInput
        {
            ActorKcId = "actor",
            Items = []
        };

        var result = await handler.BeforeExecuteAsync(process, input, _ct);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainKey("Items");
    }

    [Fact]
    public async Task AssignItems_NoLinkedRental_Fails()
    {
        IRentalActionHandler handler = new AssignItemsHandler(HandlerTestHelper.CreateStockBindingService());
        var process = HandlerTestHelper.CreateProcess(RentalStage.Approved, withRental: false);

        var input = new AssignItemsInput
        {
            ActorKcId = "actor",
            Items = [new ItemAssignment { DeviceGuid = Guid.NewGuid(), Quantity = 1 }]
        };

        var result = await handler.BeforeExecuteAsync(process, input, _ct);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainKey("Rental");
    }

    // ── RemoveItems ─────────────────────────────────────────────────

    [Fact]
    public async Task RemoveItems_EmptyGuids_Fails()
    {
        IRentalActionHandler handler = new RemoveItemsHandler(HandlerTestHelper.CreateStockBindingService());
        var process = HandlerTestHelper.CreateProcess(RentalStage.ItemsAssigned);

        var input = new RemoveItemsInput
        {
            ActorKcId = "actor",
            StockBindingGuids = []
        };

        var result = await handler.BeforeExecuteAsync(process, input, _ct);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainKey("StockBindingGuids");
    }

    // ── GenerateChecklist ───────────────────────────────────────────

    [Fact]
    public async Task GenerateChecklist_NoLinkedRental_Fails()
    {
        IRentalActionHandler handler = new GenerateChecklistHandler(_repo, HandlerTestHelper.CreateInMemoryDbContext());
        var process = HandlerTestHelper.CreateProcess(RentalStage.ItemsApproved, withRental: false);

        var input = new GenerateChecklistInput
        {
            ActorKcId = "actor",
            ChecklistType = ChecklistType.PICKUP
        };

        var result = await handler.BeforeExecuteAsync(process, input, _ct);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainKey("Rental");
    }

    // ── GenerateInvoice ─────────────────────────────────────────────

    [Fact]
    public async Task GenerateInvoice_NoLinkedRental_Fails()
    {
        IRentalActionHandler handler = new GenerateInvoiceHandler(HandlerTestHelper.CreateInMemoryDbContext());
        var process = HandlerTestHelper.CreateProcess(RentalStage.Returned, withRental: false);

        var input = new GenerateInvoiceInput
        {
            ActorKcId = "actor"
        };

        var result = await handler.BeforeExecuteAsync(process, input, _ct);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainKey("Rental");
    }

    // ── RecordPayment ───────────────────────────────────────────────

    [Fact]
    public async Task RecordPayment_ZeroAmount_Fails()
    {
        IRentalActionHandler handler = new RecordPaymentHandler(HandlerTestHelper.CreateInMemoryDbContext());
        var process = HandlerTestHelper.CreateProcess(RentalStage.Invoiced);

        var input = new RecordPaymentInput
        {
            ActorKcId = "actor",
            InvoiceGuid = Guid.NewGuid(),
            Amount = 0m,
            Method = PaymentMethod.CARD
        };

        var result = await handler.BeforeExecuteAsync(process, input, _ct);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainKey("Amount");
    }

    [Fact]
    public async Task RecordPayment_NegativeAmount_Fails()
    {
        IRentalActionHandler handler = new RecordPaymentHandler(HandlerTestHelper.CreateInMemoryDbContext());
        var process = HandlerTestHelper.CreateProcess(RentalStage.Invoiced);

        var input = new RecordPaymentInput
        {
            ActorKcId = "actor",
            InvoiceGuid = Guid.NewGuid(),
            Amount = -10m,
            Method = PaymentMethod.CARD
        };

        var result = await handler.BeforeExecuteAsync(process, input, _ct);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainKey("Amount");
    }

    // ── RecordDamages ───────────────────────────────────────────────

    [Fact]
    public async Task RecordDamages_EmptyDamages_Fails()
    {
        IRentalActionHandler handler = new RecordDamagesHandler(_repo);
        var process = HandlerTestHelper.CreateProcess(RentalStage.Returned);

        var input = new RecordDamagesInput
        {
            ActorKcId = "actor",
            Damages = []
        };

        var result = await handler.BeforeExecuteAsync(process, input, _ct);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainKey("Damages");
    }

    // ── CreateMaintenanceJobs ───────────────────────────────────────

    [Fact]
    public async Task CreateMaintenanceJobs_EmptyGuids_Fails()
    {
        IRentalActionHandler handler = new CreateMaintenanceJobsHandler(_repo, HandlerTestHelper.CreateInMemoryDbContext());
        var process = HandlerTestHelper.CreateProcess(RentalStage.Returned);

        var input = new CreateMaintenanceJobsInput
        {
            ActorKcId = "actor",
            DamagedStockBindingGuids = []
        };

        var result = await handler.BeforeExecuteAsync(process, input, _ct);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainKey("DamagedStockBindingGuids");
    }

    // ── RequestExtension ────────────────────────────────────────────

    [Fact]
    public async Task RequestExtension_NoLinkedRental_Fails()
    {
        IRentalActionHandler handler = new RequestExtensionHandler(_repo);
        var process = HandlerTestHelper.CreateProcess(RentalStage.PickedUp, withRental: false);

        var input = new RequestExtensionInput
        {
            ActorKcId = "actor",
            NewRequestedEnd = SystemClock.Instance.GetCurrentInstant() + Duration.FromDays(14)
        };

        var result = await handler.BeforeExecuteAsync(process, input, _ct);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainKey("Rental");
    }

    [Fact]
    public async Task RequestExtension_NewEndBeforeCurrentEnd_Fails()
    {
        IRentalActionHandler handler = new RequestExtensionHandler(_repo);
        var process = HandlerTestHelper.CreateProcess(RentalStage.PickedUp);
        var currentEnd = process.Rental!.RequestedEnd;

        var input = new RequestExtensionInput
        {
            ActorKcId = "actor",
            NewRequestedEnd = currentEnd - Duration.FromDays(1) // before current end
        };

        var result = await handler.BeforeExecuteAsync(process, input, _ct);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainKey("NewRequestedEnd");
    }

    [Fact]
    public async Task RequestExtension_ValidNewEnd_Passes()
    {
        IRentalActionHandler handler = new RequestExtensionHandler(_repo);
        var process = HandlerTestHelper.CreateProcess(RentalStage.PickedUp);
        var currentEnd = process.Rental!.RequestedEnd;

        var input = new RequestExtensionInput
        {
            ActorKcId = "actor",
            NewRequestedEnd = currentEnd + Duration.FromDays(3)
        };

        var result = await handler.BeforeExecuteAsync(process, input, _ct);

        result.Success.Should().BeTrue();
    }
}
