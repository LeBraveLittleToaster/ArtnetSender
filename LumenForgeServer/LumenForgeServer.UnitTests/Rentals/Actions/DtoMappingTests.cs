using FluentAssertions;
using LumenForgeServer.Common;
using LumenForgeServer.Rentals.Dto.Command;
using LumenForgeServer.Rentals.Service.Actions;
using LumenForgeServer.Rentals.Service.Actions.Handlers;
using NodaTime;

namespace LumenForgeServer.UnitTests.Rentals.Actions;

/// <summary>
/// Verifies that every API command DTO correctly maps all properties
/// to its corresponding <see cref="ActionInput"/> via <c>ToActionInput()</c>.
/// </summary>
public class DtoMappingTests
{
    // ── CreateRentalDto ─────────────────────────────────────────────

    [Fact]
    public void CreateRentalDto_MapsAllProperties()
    {
        var now = SystemClock.Instance.GetCurrentInstant();
        var dto = new CreateRentalDto
        {
            CustomerName = "Jane Doe",
            CustomerEmail = "jane@example.com",
            Purpose = "Conference",
            RequestedStart = now + Duration.FromDays(1),
            RequestedEnd = now + Duration.FromDays(5),
            Notes = "Handle with care"
        };

        var input = dto.ToActionInput();

        input.Should().BeOfType<CreateRentalInput>();
        input.CustomerName.Should().Be("Jane Doe");
        input.CustomerEmail.Should().Be("jane@example.com");
        input.Purpose.Should().Be("Conference");
        input.RequestedStart.Should().Be(dto.RequestedStart);
        input.RequestedEnd.Should().Be(dto.RequestedEnd);
        input.Notes.Should().Be("Handle with care");
    }

    [Fact]
    public void CreateRentalDto_MapsNullOptionalProperties()
    {
        var now = SystemClock.Instance.GetCurrentInstant();
        var dto = new CreateRentalDto
        {
            RequestedStart = now + Duration.FromDays(1),
            RequestedEnd = now + Duration.FromDays(5)
        };

        var input = dto.ToActionInput();

        input.CustomerName.Should().BeNull();
        input.CustomerEmail.Should().BeNull();
        input.Purpose.Should().BeNull();
        input.Notes.Should().BeNull();
    }

    // ── ApproveRequestDto ───────────────────────────────────────────

    [Fact]
    public void ApproveRequestDto_MapsComment()
    {
        var dto = new ApproveRequestDto { Comment = "Looks good" };

        var input = dto.ToActionInput();

        input.Should().BeOfType<ApproveRequestInput>();
        input.Comment.Should().Be("Looks good");
    }

    [Fact]
    public void ApproveRequestDto_MapsNullComment()
    {
        var dto = new ApproveRequestDto();

        var input = dto.ToActionInput();

        input.Comment.Should().BeNull();
    }

    // ── RejectRequestDto ────────────────────────────────────────────

    [Fact]
    public void RejectRequestDto_MapsReason()
    {
        var dto = new RejectRequestDto { Reason = "Not eligible" };

        var input = dto.ToActionInput();

        input.Should().BeOfType<RejectRequestInput>();
        input.Reason.Should().Be("Not eligible");
    }

    // ── AssignItemsDto ──────────────────────────────────────────────

    [Fact]
    public void AssignItemsDto_MapsItemsList()
    {
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();
        var dto = new AssignItemsDto
        {
            Items =
            [
                new ItemAssignmentDto { DeviceGuid = guid1, Quantity = 2 },
                new ItemAssignmentDto { DeviceGuid = guid2, Quantity = 5 }
            ]
        };

        var input = dto.ToActionInput();

        input.Should().BeOfType<AssignItemsInput>();
        input.Items.Should().HaveCount(2);
        input.Items[0].DeviceGuid.Should().Be(guid1);
        input.Items[0].Quantity.Should().Be(2);
        input.Items[1].DeviceGuid.Should().Be(guid2);
        input.Items[1].Quantity.Should().Be(5);
    }

    [Fact]
    public void AssignItemsDto_MapsEmptyList()
    {
        var dto = new AssignItemsDto { Items = [] };

        var input = dto.ToActionInput();

        input.Items.Should().BeEmpty();
    }

    // ── RemoveItemsDto ──────────────────────────────────────────────

    [Fact]
    public void RemoveItemsDto_MapsGuids()
    {
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();
        var dto = new RemoveItemsDto { StockBindingGuids = [guid1, guid2] };

        var input = dto.ToActionInput();

        input.Should().BeOfType<RemoveItemsInput>();
        input.StockBindingGuids.Should().BeEquivalentTo([guid1, guid2]);
    }

    // ── ApproveItemsDto ─────────────────────────────────────────────

    [Fact]
    public void ApproveItemsDto_MapsComment()
    {
        var dto = new ApproveItemsDto { Comment = "All good" };

        var input = dto.ToActionInput();

        input.Should().BeOfType<ApproveItemsInput>();
        input.Comment.Should().Be("All good");
    }

    // ── RejectItemsDto ──────────────────────────────────────────────

    [Fact]
    public void RejectItemsDto_MapsReason()
    {
        var dto = new RejectItemsDto { Reason = "Wrong items" };

        var input = dto.ToActionInput();

        input.Should().BeOfType<RejectItemsInput>();
        input.Reason.Should().Be("Wrong items");
    }

    // ── GenerateChecklistDto ────────────────────────────────────────

    [Fact]
    public void GenerateChecklistDto_MapsChecklistType()
    {
        var dto = new GenerateChecklistDto { ChecklistType = ChecklistType.PICKUP };

        var input = dto.ToActionInput();

        input.Should().BeOfType<GenerateChecklistInput>();
        input.ChecklistType.Should().Be(ChecklistType.PICKUP);
    }

    // ── ScanChecklistDto ────────────────────────────────────────────

    [Fact]
    public void ScanChecklistDto_MapsAllProperties()
    {
        var guid = Guid.NewGuid();
        var dto = new ScanChecklistDto { ChecklistGuid = guid, ScannedValue = "SN-12345" };

        var input = dto.ToActionInput();

        input.Should().BeOfType<ScanChecklistInput>();
        input.ChecklistGuid.Should().Be(guid);
        input.ScannedValue.Should().Be("SN-12345");
    }

    // ── SignChecklistDto ────────────────────────────────────────────

    [Fact]
    public void SignChecklistDto_MapsAllProperties()
    {
        var guid = Guid.NewGuid();
        var dto = new SignChecklistDto { ChecklistGuid = guid, SignatureData = "base64data" };

        var input = dto.ToActionInput();

        input.Should().BeOfType<SignChecklistInput>();
        input.ChecklistGuid.Should().Be(guid);
        input.SignatureData.Should().Be("base64data");
    }

    // ── RecordPickupDto ─────────────────────────────────────────────

    [Fact]
    public void RecordPickupDto_MapsNotes()
    {
        var dto = new RecordPickupDto { Notes = "On time" };

        var input = dto.ToActionInput();

        input.Should().BeOfType<RecordPickupInput>();
        input.Notes.Should().Be("On time");
    }

    [Fact]
    public void RecordPickupDto_MapsNullNotes()
    {
        var dto = new RecordPickupDto();

        var input = dto.ToActionInput();

        input.Notes.Should().BeNull();
    }

    // ── RecordReturnDto ─────────────────────────────────────────────

    [Fact]
    public void RecordReturnDto_MapsNotes()
    {
        var dto = new RecordReturnDto { Notes = "Items in good condition" };

        var input = dto.ToActionInput();

        input.Should().BeOfType<RecordReturnInput>();
        input.Notes.Should().Be("Items in good condition");
    }

    // ── RequestExtensionDto ─────────────────────────────────────────

    [Fact]
    public void RequestExtensionDto_MapsAllProperties()
    {
        var newEnd = SystemClock.Instance.GetCurrentInstant() + Duration.FromDays(14);
        var dto = new RequestExtensionDto { NewRequestedEnd = newEnd, Reason = "Project delayed" };

        var input = dto.ToActionInput();

        input.Should().BeOfType<RequestExtensionInput>();
        input.NewRequestedEnd.Should().Be(newEnd);
        input.Reason.Should().Be("Project delayed");
    }

    [Fact]
    public void RequestExtensionDto_MapsNullReason()
    {
        var newEnd = SystemClock.Instance.GetCurrentInstant() + Duration.FromDays(14);
        var dto = new RequestExtensionDto { NewRequestedEnd = newEnd };

        var input = dto.ToActionInput();

        input.Reason.Should().BeNull();
    }

    // ── ApproveExtensionDto ─────────────────────────────────────────

    [Fact]
    public void ApproveExtensionDto_MapsAllProperties()
    {
        var guid = Guid.NewGuid();
        var dto = new ApproveExtensionDto { ExtensionGuid = guid, Comment = "Approved" };

        var input = dto.ToActionInput();

        input.Should().BeOfType<ApproveExtensionInput>();
        input.ExtensionGuid.Should().Be(guid);
        input.Comment.Should().Be("Approved");
    }

    // ── RejectExtensionDto ──────────────────────────────────────────

    [Fact]
    public void RejectExtensionDto_MapsAllProperties()
    {
        var guid = Guid.NewGuid();
        var dto = new RejectExtensionDto { ExtensionGuid = guid, Reason = "Cannot extend" };

        var input = dto.ToActionInput();

        input.Should().BeOfType<RejectExtensionInput>();
        input.ExtensionGuid.Should().Be(guid);
        input.Reason.Should().Be("Cannot extend");
    }

    // ── RecordDamagesDto ────────────────────────────────────────────

    [Fact]
    public void RecordDamagesDto_MapsAllProperties()
    {
        var sbGuid1 = Guid.NewGuid();
        var sbGuid2 = Guid.NewGuid();
        var dto = new RecordDamagesDto
        {
            Damages =
            [
                new DamageEntryDto
                {
                    StockBindingGuid = sbGuid1,
                    Description = "Screen cracked",
                    Severity = DamageSeverity.SEVERE
                },
                new DamageEntryDto
                {
                    StockBindingGuid = sbGuid2,
                    Description = "Minor scratch",
                    Severity = DamageSeverity.MINOR
                }
            ]
        };

        var input = dto.ToActionInput();

        input.Should().BeOfType<RecordDamagesInput>();
        input.Damages.Should().HaveCount(2);
        input.Damages[0].StockBindingGuid.Should().Be(sbGuid1);
        input.Damages[0].Description.Should().Be("Screen cracked");
        input.Damages[0].Severity.Should().Be(DamageSeverity.SEVERE);
        input.Damages[1].StockBindingGuid.Should().Be(sbGuid2);
        input.Damages[1].Description.Should().Be("Minor scratch");
        input.Damages[1].Severity.Should().Be(DamageSeverity.MINOR);
    }

    // ── CreateMaintenanceJobsDto ────────────────────────────────────

    [Fact]
    public void CreateMaintenanceJobsDto_MapsGuids()
    {
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();
        var dto = new CreateMaintenanceJobsDto { DamagedStockBindingGuids = [guid1, guid2] };

        var input = dto.ToActionInput();

        input.Should().BeOfType<CreateMaintenanceJobsInput>();
        input.DamagedStockBindingGuids.Should().BeEquivalentTo([guid1, guid2]);
    }

    // ── GenerateInvoiceDto ──────────────────────────────────────────

    [Fact]
    public void GenerateInvoiceDto_MapsDueDateOverride()
    {
        var dto = new GenerateInvoiceDto { DueDateOverride = "2025-12-31T00:00:00Z" };

        var input = dto.ToActionInput();

        input.Should().BeOfType<GenerateInvoiceInput>();
        input.DueDateOverride.Should().Be("2025-12-31T00:00:00Z");
    }

    [Fact]
    public void GenerateInvoiceDto_MapsNullDueDateOverride()
    {
        var dto = new GenerateInvoiceDto();

        var input = dto.ToActionInput();

        input.DueDateOverride.Should().BeNull();
    }

    // ── RecordPaymentDto ────────────────────────────────────────────

    [Fact]
    public void RecordPaymentDto_MapsAllProperties()
    {
        var invoiceGuid = Guid.NewGuid();
        var dto = new RecordPaymentDto
        {
            InvoiceGuid = invoiceGuid,
            Amount = 150.50m,
            Method = PaymentMethod.CARD,
            Reference = "TXN-12345"
        };

        var input = dto.ToActionInput();

        input.Should().BeOfType<RecordPaymentInput>();
        input.InvoiceGuid.Should().Be(invoiceGuid);
        input.Amount.Should().Be(150.50m);
        input.Method.Should().Be(PaymentMethod.CARD);
        input.Reference.Should().Be("TXN-12345");
    }

    [Fact]
    public void RecordPaymentDto_MapsNullReference()
    {
        var dto = new RecordPaymentDto
        {
            InvoiceGuid = Guid.NewGuid(),
            Amount = 100m,
            Method = PaymentMethod.TRANSFER
        };

        var input = dto.ToActionInput();

        input.Reference.Should().BeNull();
    }

    // ── GenerateReportDto ───────────────────────────────────────────

    [Fact]
    public void GenerateReportDto_MapsAllProperties()
    {
        var dto = new GenerateReportDto { IncludeDamages = false, IncludePayments = false };

        var input = dto.ToActionInput();

        input.Should().BeOfType<GenerateReportInput>();
        input.IncludeDamages.Should().BeFalse();
        input.IncludePayments.Should().BeFalse();
    }

    [Fact]
    public void GenerateReportDto_DefaultsToTrue()
    {
        var dto = new GenerateReportDto();

        var input = dto.ToActionInput();

        input.IncludeDamages.Should().BeTrue();
        input.IncludePayments.Should().BeTrue();
    }

    // ── CompleteRentalDto ───────────────────────────────────────────

    [Fact]
    public void CompleteRentalDto_MapsComment()
    {
        var dto = new CompleteRentalDto { Comment = "All done" };

        var input = dto.ToActionInput();

        input.Should().BeOfType<CompleteRentalInput>();
        input.Comment.Should().Be("All done");
    }

    // ── CancelRentalDto ─────────────────────────────────────────────

    [Fact]
    public void CancelRentalDto_MapsReason()
    {
        var dto = new CancelRentalDto { Reason = "Budget cuts" };

        var input = dto.ToActionInput();

        input.Should().BeOfType<CancelRentalInput>();
        input.Reason.Should().Be("Budget cuts");
    }

    // ── ScrapRentalDto ──────────────────────────────────────────────

    [Fact]
    public void ScrapRentalDto_MapsReason()
    {
        var dto = new ScrapRentalDto { Reason = "Total loss" };

        var input = dto.ToActionInput();

        input.Should().BeOfType<ScrapRentalInput>();
        input.Reason.Should().Be("Total loss");
    }

    // ── Cross-cutting: ActorKcId is never set by DTO mapping ────────

    [Fact]
    public void ToActionInput_NeverSetsActorKcId()
    {
        var now = SystemClock.Instance.GetCurrentInstant();

        var inputs = new List<ActionInput>
        {
            new CreateRentalDto { RequestedStart = now, RequestedEnd = now + Duration.FromDays(1) }.ToActionInput(),
            new ApproveRequestDto().ToActionInput(),
            new RejectRequestDto { Reason = "r" }.ToActionInput(),
            new AssignItemsDto { Items = [] }.ToActionInput(),
            new RemoveItemsDto { StockBindingGuids = [] }.ToActionInput(),
            new ApproveItemsDto().ToActionInput(),
            new RejectItemsDto { Reason = "r" }.ToActionInput(),
            new GenerateChecklistDto { ChecklistType = ChecklistType.PICKUP }.ToActionInput(),
            new ScanChecklistDto { ChecklistGuid = Guid.NewGuid(), ScannedValue = "v" }.ToActionInput(),
            new SignChecklistDto { ChecklistGuid = Guid.NewGuid(), SignatureData = "s" }.ToActionInput(),
            new RecordPickupDto().ToActionInput(),
            new RecordReturnDto().ToActionInput(),
            new RequestExtensionDto { NewRequestedEnd = now + Duration.FromDays(14) }.ToActionInput(),
            new ApproveExtensionDto { ExtensionGuid = Guid.NewGuid() }.ToActionInput(),
            new RejectExtensionDto { ExtensionGuid = Guid.NewGuid(), Reason = "r" }.ToActionInput(),
            new RecordDamagesDto { Damages = [new DamageEntryDto { StockBindingGuid = Guid.NewGuid(), Description = "d", Severity = DamageSeverity.MINOR }] }.ToActionInput(),
            new CreateMaintenanceJobsDto { DamagedStockBindingGuids = [Guid.NewGuid()] }.ToActionInput(),
            new GenerateInvoiceDto().ToActionInput(),
            new RecordPaymentDto { InvoiceGuid = Guid.NewGuid(), Amount = 1m, Method = PaymentMethod.CARD }.ToActionInput(),
            new GenerateReportDto().ToActionInput(),
            new CompleteRentalDto().ToActionInput(),
            new CancelRentalDto { Reason = "r" }.ToActionInput(),
            new ScrapRentalDto { Reason = "r" }.ToActionInput()
        };

        foreach (var input in inputs)
        {
            input.ActorKcId.Should().Be(string.Empty,
                because: $"{input.GetType().Name}.ActorKcId must retain its default (empty) value after DTO mapping — the controller sets it from the JWT claim");
        }
    }
}
