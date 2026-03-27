using FluentAssertions;
using LumenForgeServer.Common;
using LumenForgeServer.Common.Exceptions;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Dto.Command;
using LumenForgeServer.Rentals.Persistence;
using LumenForgeServer.UnitTests.Rentals.Actions.Helpers;
using LumenForgeServer.Rentals.Service.Actions;
using LumenForgeServer.Rentals.Service.Actions.Handlers;
using NodaTime;
using NSubstitute;

namespace LumenForgeServer.UnitTests.Rentals.Actions.Handlers;

/// <summary>
/// Tests the <c>ExecuteAsync</c> logic for handlers that either have no
/// external dependencies or depend only on <see cref="IRentalProcessRepository"/>
/// (easily mocked).
/// </summary>
public class HandlerExecutionTests
{
    private readonly CancellationToken _ct = CancellationToken.None;
    private readonly IRentalProcessRepository _repo = Substitute.For<IRentalProcessRepository>();
    private readonly IQuestionRepository _questionRepo = Substitute.For<IQuestionRepository>();

    // ── ApproveRequest ──────────────────────────────────────────────

    [Fact]
    public async Task ApproveRequest_TransitionsToApproved()
    {
        IRentalActionHandler handler = new ApproveRequestHandler();
        var process = HandlerTestHelper.CreateProcess(RentalStage.Requested);

        var input = new ApproveRequestInput { ActorKcId = "approver", Comment = "Looks good" };
        var result = await handler.ExecuteAsync(process, input, _ct);

        result.Success.Should().BeTrue();
        result.NewStage.Should().Be(RentalStage.Approved);
    }

    // ── RejectRequest ───────────────────────────────────────────────

    [Fact]
    public async Task RejectRequest_TransitionsToCancelled()
    {
        IRentalActionHandler handler = new RejectRequestHandler();
        var process = HandlerTestHelper.CreateProcess(RentalStage.Requested);

        var input = new RejectRequestInput { ActorKcId = "approver", Reason = "Not eligible" };
        var result = await handler.ExecuteAsync(process, input, _ct);

        result.Success.Should().BeTrue();
        result.NewStage.Should().Be(RentalStage.Cancelled);
    }

    // ── ApproveItems ────────────────────────────────────────────────

    [Fact]
    public async Task ApproveItems_TransitionsToItemsApproved()
    {
        IRentalActionHandler handler = new ApproveItemsHandler();
        var process = HandlerTestHelper.CreateProcess(RentalStage.ItemsAssigned);

        var input = new ApproveItemsInput { ActorKcId = "approver" };
        var result = await handler.ExecuteAsync(process, input, _ct);

        result.Success.Should().BeTrue();
        result.NewStage.Should().Be(RentalStage.ItemsApproved);
    }

    // ── RejectItems ─────────────────────────────────────────────────

    [Fact]
    public async Task RejectItems_TransitionsBackToApproved()
    {
        IRentalActionHandler handler = new RejectItemsHandler();
        var process = HandlerTestHelper.CreateProcess(RentalStage.ItemsAssigned);

        var input = new RejectItemsInput { ActorKcId = "approver", Reason = "Wrong items" };
        var result = await handler.ExecuteAsync(process, input, _ct);

        result.Success.Should().BeTrue();
        result.NewStage.Should().Be(RentalStage.Approved);
    }

    // ── RecordPickup ────────────────────────────────────────────────

    [Fact]
    public async Task RecordPickup_TransitionsToPickedUp()
    {
        IRentalActionHandler handler = new RecordPickupHandler();
        var process = HandlerTestHelper.CreateProcess(RentalStage.ReadyForPickup);

        var input = new RecordPickupInput { ActorKcId = "staff" };
        var result = await handler.ExecuteAsync(process, input, _ct);

        result.Success.Should().BeTrue();
        result.NewStage.Should().Be(RentalStage.PickedUp);
    }

    [Fact]
    public async Task RecordPickup_WithNotes_AppendsToRentalNotes()
    {
        IRentalActionHandler handler = new RecordPickupHandler();
        var process = HandlerTestHelper.CreateProcess(RentalStage.ReadyForPickup);
        process.Rental!.Notes = "Existing notes";

        var input = new RecordPickupInput { ActorKcId = "staff", Notes = "Customer arrived on time" };
        await handler.ExecuteAsync(process, input, _ct);

        process.Rental.Notes.Should().Contain("[Pickup]");
        process.Rental.Notes.Should().Contain("Customer arrived on time");
        process.Rental.Notes.Should().StartWith("Existing notes");
    }

    [Fact]
    public async Task RecordPickup_WithNotes_SetsNotesWhenEmpty()
    {
        IRentalActionHandler handler = new RecordPickupHandler();
        var process = HandlerTestHelper.CreateProcess(RentalStage.ReadyForPickup);
        process.Rental!.Notes = null;

        var input = new RecordPickupInput { ActorKcId = "staff", Notes = "First note" };
        await handler.ExecuteAsync(process, input, _ct);

        process.Rental.Notes.Should().Be("[Pickup] First note");
    }

    // ── RecordReturn ────────────────────────────────────────────────

    [Fact]
    public async Task RecordReturn_TransitionsToReturned()
    {
        IRentalActionHandler handler = new RecordReturnHandler();
        var process = HandlerTestHelper.CreateProcess(RentalStage.PickedUp);

        var input = new RecordReturnInput { ActorKcId = "staff" };
        var result = await handler.ExecuteAsync(process, input, _ct);

        result.Success.Should().BeTrue();
        result.NewStage.Should().Be(RentalStage.Returned);
    }

    [Fact]
    public async Task RecordReturn_WithNotes_AppendsToRentalNotes()
    {
        IRentalActionHandler handler = new RecordReturnHandler();
        var process = HandlerTestHelper.CreateProcess(RentalStage.PickedUp);
        process.Rental!.Notes = null;

        var input = new RecordReturnInput { ActorKcId = "staff", Notes = "Items in good condition" };
        await handler.ExecuteAsync(process, input, _ct);

        process.Rental.Notes.Should().Be("[Return] Items in good condition");
    }

    // ── CompleteRental ──────────────────────────────────────────────

    [Fact]
    public async Task CompleteRental_TransitionsToCompleted()
    {
        IRentalActionHandler handler = new CompleteRentalHandler();
        var process = HandlerTestHelper.CreateProcess(RentalStage.Paid);

        var input = new CompleteRentalInput { ActorKcId = "staff" };
        var result = await handler.ExecuteAsync(process, input, _ct);

        result.Success.Should().BeTrue();
        result.NewStage.Should().Be(RentalStage.Completed);
    }

    [Fact]
    public async Task CompleteRental_WithComment_AppendsToNotes()
    {
        IRentalActionHandler handler = new CompleteRentalHandler();
        var process = HandlerTestHelper.CreateProcess(RentalStage.Paid);
        process.Rental!.Notes = null;

        var input = new CompleteRentalInput { ActorKcId = "staff", Comment = "All done" };
        await handler.ExecuteAsync(process, input, _ct);

        process.Rental.Notes.Should().Be("[Completed] All done");
    }

    // ── CancelRental ────────────────────────────────────────────────

    [Theory]
    [InlineData(RentalStage.Requested)]
    [InlineData(RentalStage.Approved)]
    [InlineData(RentalStage.ItemsAssigned)]
    [InlineData(RentalStage.ItemsApproved)]
    [InlineData(RentalStage.ReadyForPickup)]
    public async Task CancelRental_AllowedInMultipleStages(RentalStage stage)
    {
        IRentalActionHandler handler = new CancelRentalHandler();
        var process = HandlerTestHelper.CreateProcess(stage);

        var input = new CancelRentalInput { ActorKcId = "staff", Reason = "Customer request" };
        var result = await handler.ExecuteAsync(process, input, _ct);

        result.Success.Should().BeTrue();
        result.NewStage.Should().Be(RentalStage.Cancelled);
    }

    [Fact]
    public async Task CancelRental_AppendsReasonToNotes()
    {
        IRentalActionHandler handler = new CancelRentalHandler();
        var process = HandlerTestHelper.CreateProcess(RentalStage.Requested);
        process.Rental!.Notes = null;

        var input = new CancelRentalInput { ActorKcId = "staff", Reason = "Budget cuts" };
        await handler.ExecuteAsync(process, input, _ct);

        process.Rental.Notes.Should().Be("[Cancelled] Budget cuts");
    }

    // ── ScrapRental ─────────────────────────────────────────────────

    [Fact]
    public async Task ScrapRental_TransitionsToScrapped()
    {
        IRentalActionHandler handler = new ScrapRentalHandler();
        var process = HandlerTestHelper.CreateProcess(RentalStage.PickedUp);

        var input = new ScrapRentalInput { ActorKcId = "staff", Reason = "Total loss" };
        var result = await handler.ExecuteAsync(process, input, _ct);

        result.Success.Should().BeTrue();
        result.NewStage.Should().Be(RentalStage.Scrapped);
    }

    [Fact]
    public async Task ScrapRental_AppendsReasonToNotes()
    {
        IRentalActionHandler handler = new ScrapRentalHandler();
        var process = HandlerTestHelper.CreateProcess(RentalStage.PickedUp);
        process.Rental!.Notes = "Previous note";

        var input = new ScrapRentalInput { ActorKcId = "staff", Reason = "Total loss" };
        await handler.ExecuteAsync(process, input, _ct);

        process.Rental.Notes.Should().Contain("[Scrapped] Total loss");
        process.Rental.Notes.Should().StartWith("Previous note");
    }

    // ── CreateRental (with mocked repo) ─────────────────────────────

    [Fact]
    public async Task CreateRental_CreatesRentalAndTransitionsToRequested()
    {
        IRentalActionHandler handler = new CreateRentalHandler(_repo, _questionRepo);
        var process = HandlerTestHelper.CreateProcess(RentalStage.None, withRental: false);
        var now = SystemClock.Instance.GetCurrentInstant();
        var questionGuid1 = Guid.NewGuid();
        var questionGuid2 = Guid.NewGuid();

        _questionRepo.GetQuestionIdsByGuidAsync(Arg.Any<List<Guid>>()).Returns(new Dictionary<Guid, long>
        {
            [questionGuid1] = 101,
            [questionGuid2] = 102
        });

        var input = new CreateRentalInput
        {
            ActorKcId = "customer-kc-id",
            CustomerName = "Jane Doe",
            CustomerEmail = "jane@example.com",
            Purpose = "Conference equipment",
            RequestedStart = now + Duration.FromDays(1),
            RequestedEnd = now + Duration.FromDays(5),
            Notes = "Handle with care",
            QASets =
            [
                new QASet { Guid = questionGuid1.ToString(), Value = "A1" },
                new QASet { Guid = questionGuid2.ToString(), Value = "A2" }
            ]
        };

        var result = await handler.ExecuteAsync(process, input, _ct);

        result.Success.Should().BeTrue();
        result.NewStage.Should().Be(RentalStage.Requested);
        result.Should().BeOfType<CreateRentalResult>();
        ((CreateRentalResult)result).ProcessInstanceGuid.Should().Be(process.Guid);

        process.Rental.Should().NotBeNull();
        process.Rental!.CustomerKcId.Should().Be("customer-kc-id");
        process.Rental.CustomerName.Should().Be("Jane Doe");
        process.Rental.Notes.Should().Be("Handle with care");
        process.Rental.Answers.Should().HaveCount(2);
        process.Rental.Answers[0].QuestionId.Should().Be(101);
        process.Rental.Answers[0].Value.Should().Be("A1");
        process.Rental.Answers[1].QuestionId.Should().Be(102);
        process.Rental.Answers[1].Value.Should().Be("A2");

        await _repo.Received(1).AddRentalAsync(Arg.Any<Rental>(), _ct);
    }

    // ── ScanChecklist (with mocked repo) ────────────────────────────

    [Fact]
    public async Task ScanChecklist_MarksFirstUnscannedItem()
    {
        var checklist = HandlerTestHelper.CreateChecklist(1, itemCount: 3);
        var checklistGuid = checklist.Guid;

        _repo.GetChecklistByGuidAsync(checklistGuid, _ct).Returns(checklist);

        IRentalActionHandler handler = new ScanChecklistHandler(_repo);
        var process = HandlerTestHelper.CreateProcess(RentalStage.ReadyForPickup);

        var input = new ScanChecklistInput
        {
            ActorKcId = "scanner",
            ChecklistGuid = checklistGuid,
            ScannedValue = "SN-12345"
        };

        var result = await handler.ExecuteAsync(process, input, _ct);

        result.Success.Should().BeTrue();
        result.NewStage.Should().BeNull(); // no stage change

        checklist.Items[0].IsScanned.Should().BeTrue();
        checklist.Items[0].ScannedValue.Should().Be("SN-12345");
        checklist.Items[0].ScannedByKcId.Should().Be("scanner");
        checklist.Items[1].IsScanned.Should().BeFalse();
    }

    [Fact]
    public async Task ScanChecklist_AllItemsAlreadyScanned_Fails()
    {
        var checklist = HandlerTestHelper.CreateChecklist(1, itemCount: 2, allScanned: true);
        _repo.GetChecklistByGuidAsync(checklist.Guid, _ct).Returns(checklist);

        IRentalActionHandler handler = new ScanChecklistHandler(_repo);
        var process = HandlerTestHelper.CreateProcess(RentalStage.ReadyForPickup);

        var input = new ScanChecklistInput
        {
            ActorKcId = "scanner",
            ChecklistGuid = checklist.Guid,
            ScannedValue = "SN-EXTRA"
        };

        var result = await handler.ExecuteAsync(process, input, _ct);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainKey("Checklist");
    }

    [Fact]
    public async Task ScanChecklist_ChecklistNotFound_ThrowsNotFound()
    {
        _repo.GetChecklistByGuidAsync(Arg.Any<Guid>(), _ct).Returns((Checklist?)null);

        IRentalActionHandler handler = new ScanChecklistHandler(_repo);
        var process = HandlerTestHelper.CreateProcess(RentalStage.ReadyForPickup);

        var input = new ScanChecklistInput
        {
            ActorKcId = "scanner",
            ChecklistGuid = Guid.NewGuid(),
            ScannedValue = "SN-12345"
        };

        var act = () => handler.ExecuteAsync(process, input, _ct);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── SignChecklist (with mocked repo) ────────────────────────────

    [Fact]
    public async Task SignChecklist_AllScanned_SignsChecklist()
    {
        var checklist = HandlerTestHelper.CreateChecklist(1, itemCount: 2, allScanned: true);
        _repo.GetChecklistByGuidAsync(checklist.Guid, _ct).Returns(checklist);

        IRentalActionHandler handler = new SignChecklistHandler(_repo);
        var process = HandlerTestHelper.CreateProcess(RentalStage.ReadyForPickup);

        var input = new SignChecklistInput
        {
            ActorKcId = "signer",
            ChecklistGuid = checklist.Guid,
            SignatureData = "base64-sig-data"
        };

        var result = await handler.ExecuteAsync(process, input, _ct);

        result.Success.Should().BeTrue();
        checklist.IsSigned.Should().BeTrue();
        checklist.SignedByKcId.Should().Be("signer");
        checklist.SignatureData.Should().Be("base64-sig-data");
        checklist.SignedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SignChecklist_AlreadySigned_Fails()
    {
        var checklist = HandlerTestHelper.CreateChecklist(1, itemCount: 1, allScanned: true);
        checklist.IsSigned = true;
        checklist.SignedByKcId = "previous-signer";
        _repo.GetChecklistByGuidAsync(checklist.Guid, _ct).Returns(checklist);

        IRentalActionHandler handler = new SignChecklistHandler(_repo);
        var process = HandlerTestHelper.CreateProcess(RentalStage.ReadyForPickup);

        var input = new SignChecklistInput
        {
            ActorKcId = "signer",
            ChecklistGuid = checklist.Guid,
            SignatureData = "new-sig"
        };

        var result = await handler.ExecuteAsync(process, input, _ct);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainKey("Checklist");
    }

    [Fact]
    public async Task SignChecklist_UnscannedItems_Fails()
    {
        var checklist = HandlerTestHelper.CreateChecklist(1, itemCount: 3, allScanned: false);
        _repo.GetChecklistByGuidAsync(checklist.Guid, _ct).Returns(checklist);

        IRentalActionHandler handler = new SignChecklistHandler(_repo);
        var process = HandlerTestHelper.CreateProcess(RentalStage.ReadyForPickup);

        var input = new SignChecklistInput
        {
            ActorKcId = "signer",
            ChecklistGuid = checklist.Guid,
            SignatureData = "sig"
        };

        var result = await handler.ExecuteAsync(process, input, _ct);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainKey("Items");
    }

    // ── RequestExtension (with mocked repo) ─────────────────────────

    [Fact]
    public async Task RequestExtension_CreatesExtensionRecord()
    {
        IRentalActionHandler handler = new RequestExtensionHandler(_repo);
        var process = HandlerTestHelper.CreateProcess(RentalStage.PickedUp);
        var currentEnd = process.Rental!.RequestedEnd;

        var input = new RequestExtensionInput
        {
            ActorKcId = "customer",
            NewRequestedEnd = currentEnd + Duration.FromDays(7),
            Reason = "Project delayed"
        };

        var result = await handler.ExecuteAsync(process, input, _ct);

        result.Success.Should().BeTrue();
        result.NewStage.Should().BeNull(); // no stage change
        result.Should().BeOfType<RequestExtensionResult>();
        ((RequestExtensionResult)result).ExtensionGuid.Should().NotBeEmpty();

        await _repo.Received(1).AddExtensionAsync(
            Arg.Is<RentalExtension>(e =>
                e.NewRequestedEnd == currentEnd + Duration.FromDays(7) &&
                e.Reason == "Project delayed" &&
                e.RequestedByKcId == "customer"),
            _ct);
    }

    // ── ApproveExtension (with mocked repo) ─────────────────────────

    [Fact]
    public async Task ApproveExtension_ApprovesAndUpdatesRentalEndDate()
    {
        var process = HandlerTestHelper.CreateProcess(RentalStage.PickedUp);
        var originalEnd = process.Rental!.RequestedEnd;
        var extension = HandlerTestHelper.CreatePendingExtension(process.Id, originalEnd);

        _repo.GetExtensionByGuidAsync(extension.Guid, _ct).Returns(extension);

        IRentalActionHandler handler = new ApproveExtensionHandler(_repo);

        var input = new ApproveExtensionInput
        {
            ActorKcId = "approver",
            ExtensionGuid = extension.Guid,
            Comment = "Approved"
        };

        var result = await handler.ExecuteAsync(process, input, _ct);

        result.Success.Should().BeTrue();
        extension.IsApproved.Should().BeTrue();
        extension.ReviewedByKcId.Should().Be("approver");
        extension.ReviewComment.Should().Be("Approved");
        process.Rental.RequestedEnd.Should().Be(extension.NewRequestedEnd);
    }

    [Fact]
    public async Task ApproveExtension_AlreadyReviewed_Fails()
    {
        var process = HandlerTestHelper.CreateProcess(RentalStage.PickedUp);
        var extension = HandlerTestHelper.CreatePendingExtension(process.Id, process.Rental!.RequestedEnd);
        extension.IsApproved = true; // already reviewed

        _repo.GetExtensionByGuidAsync(extension.Guid, _ct).Returns(extension);

        IRentalActionHandler handler = new ApproveExtensionHandler(_repo);

        var input = new ApproveExtensionInput
        {
            ActorKcId = "approver",
            ExtensionGuid = extension.Guid
        };

        var result = await handler.ExecuteAsync(process, input, _ct);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainKey("Extension");
    }

    // ── RejectExtension (with mocked repo) ──────────────────────────

    [Fact]
    public async Task RejectExtension_RejectsExtension()
    {
        var process = HandlerTestHelper.CreateProcess(RentalStage.PickedUp);
        var extension = HandlerTestHelper.CreatePendingExtension(process.Id, process.Rental!.RequestedEnd);

        _repo.GetExtensionByGuidAsync(extension.Guid, _ct).Returns(extension);

        IRentalActionHandler handler = new RejectExtensionHandler(_repo);

        var input = new RejectExtensionInput
        {
            ActorKcId = "approver",
            ExtensionGuid = extension.Guid,
            Reason = "Cannot extend"
        };

        var result = await handler.ExecuteAsync(process, input, _ct);

        result.Success.Should().BeTrue();
        extension.IsApproved.Should().BeFalse();
        extension.ReviewComment.Should().Be("Cannot extend");
    }

    [Fact]
    public async Task RejectExtension_AlreadyReviewed_Fails()
    {
        var process = HandlerTestHelper.CreateProcess(RentalStage.PickedUp);
        var extension = HandlerTestHelper.CreatePendingExtension(process.Id, process.Rental!.RequestedEnd);
        extension.IsApproved = false; // already reviewed

        _repo.GetExtensionByGuidAsync(extension.Guid, _ct).Returns(extension);

        IRentalActionHandler handler = new RejectExtensionHandler(_repo);

        var input = new RejectExtensionInput
        {
            ActorKcId = "approver",
            ExtensionGuid = extension.Guid,
            Reason = "Nope"
        };

        var result = await handler.ExecuteAsync(process, input, _ct);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainKey("Extension");
    }

    // ── RecordDamages (with mocked repo) ────────────────────────────

    [Fact]
    public async Task RecordDamages_CreatesDamageReportsAndTransitionsToInspected()
    {
        IRentalActionHandler handler = new RecordDamagesHandler(_repo);
        var process = HandlerTestHelper.CreateProcess(RentalStage.Returned);

        var input = new RecordDamagesInput
        {
            ActorKcId = "inspector",
            Damages =
            [
                new DamageEntry
                {
                    StockBindingGuid = Guid.NewGuid(),
                    Description = "Screen cracked",
                    Severity = DamageSeverity.SEVERE
                },
                new DamageEntry
                {
                    StockBindingGuid = Guid.NewGuid(),
                    Description = "Minor scratch",
                    Severity = DamageSeverity.MINOR
                }
            ]
        };

        var result = await handler.ExecuteAsync(process, input, _ct);

        result.Success.Should().BeTrue();
        result.NewStage.Should().Be(RentalStage.Inspected);

        await _repo.Received(1).AddDamageReportsAsync(
            Arg.Is<List<RentalDamageReport>>(reports =>
                reports.Count == 2 &&
                reports[0].Description == "Screen cracked" &&
                reports[0].ReportedByKcId == "inspector" &&
                reports[1].Description == "Minor scratch"),
            _ct);
    }
}
