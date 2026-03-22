using FluentAssertions;
using LumenForgeServer.Common;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Dto.Query;
using LumenForgeServer.Rentals.Dto.View;
using LumenForgeServer.Rentals.Service.Actions;
using NodaTime;

namespace LumenForgeServer.UnitTests.Rentals.Views;

/// <summary>
/// Verifies that all view DTO <c>FromEntity</c> methods correctly map
/// every property from the domain entity.
/// </summary>
public class ViewMappingTests
{
    private static readonly Instant Now = SystemClock.Instance.GetCurrentInstant();

    // ── RentalView ──────────────────────────────────────────────────

    [Fact]
    public void RentalView_FromEntity_MapsAllProperties()
    {
        var rental = CreateRental();

        var view = RentalView.FromEntity(rental);

        view.Uuid.Should().Be(rental.Uuid);
        view.CustomerKcId.Should().Be(rental.CustomerKcId);
        view.CustomerName.Should().Be(rental.CustomerName);
        view.CustomerEmail.Should().Be(rental.CustomerEmail);
        view.Purpose.Should().Be(rental.Purpose);
        view.RequestedStart.Should().Be(rental.RequestedStart);
        view.RequestedEnd.Should().Be(rental.RequestedEnd);
        view.Priority.Should().Be(rental.Priority);
        view.Notes.Should().Be(rental.Notes);
        view.CreatedAt.Should().Be(rental.CreatedAt);
        view.UpdatedAt.Should().Be(rental.UpdatedAt);
    }

    // ── RentalProcessView ───────────────────────────────────────────

    [Fact]
    public void RentalProcessView_FromEntity_MapsBaseProperties()
    {
        var process = CreateProcess();

        var view = RentalProcessView.FromEntity(process);

        view.Guid.Should().Be(process.Guid);
        view.CurrentStage.Should().Be(process.CurrentStage);
        view.CreatedByKcId.Should().Be(process.CreatedByKcId);
        view.CreatedAt.Should().Be(process.CreatedAt);
        view.UpdatedAt.Should().Be(process.UpdatedAt);
        view.Rental.Should().NotBeNull();
    }

    [Fact]
    public void RentalProcessView_WithNoIncludes_OmitsNestedCollections()
    {
        var process = CreateProcess();

        var view = RentalProcessView.FromEntity(process, RentalProcessInclude.None);

        view.Checklists.Should().BeNull();
        view.Extensions.Should().BeNull();
        view.DamageReports.Should().BeNull();
    }

    [Fact]
    public void RentalProcessView_WithChecklistsInclude_PopulatesChecklists()
    {
        var process = CreateProcess();
        process.Checklists.Add(CreateChecklist(process.Id));

        var view = RentalProcessView.FromEntity(process, RentalProcessInclude.Checklists);

        view.Checklists.Should().HaveCount(1);
        view.Extensions.Should().BeNull();
        view.DamageReports.Should().BeNull();
    }

    [Fact]
    public void RentalProcessView_WithExtensionsInclude_PopulatesExtensions()
    {
        var process = CreateProcess();
        process.Extensions.Add(CreateExtension(process.Id));

        var view = RentalProcessView.FromEntity(process, RentalProcessInclude.Extensions);

        view.Extensions.Should().HaveCount(1);
        view.Checklists.Should().BeNull();
        view.DamageReports.Should().BeNull();
    }

    [Fact]
    public void RentalProcessView_WithDamageReportsInclude_PopulatesDamageReports()
    {
        var process = CreateProcess();
        process.DamageReports.Add(CreateDamageReport(process.Id));

        var view = RentalProcessView.FromEntity(process, RentalProcessInclude.DamageReports);

        view.DamageReports.Should().HaveCount(1);
        view.Checklists.Should().BeNull();
        view.Extensions.Should().BeNull();
    }

    [Fact]
    public void RentalProcessView_WithAllIncludes_PopulatesEverything()
    {
        var process = CreateProcess();
        process.Checklists.Add(CreateChecklist(process.Id));
        process.Extensions.Add(CreateExtension(process.Id));
        process.DamageReports.Add(CreateDamageReport(process.Id));

        var view = RentalProcessView.FromEntity(process, RentalProcessInclude.All);

        view.Checklists.Should().HaveCount(1);
        view.Extensions.Should().HaveCount(1);
        view.DamageReports.Should().HaveCount(1);
    }

    [Fact]
    public void RentalProcessView_BoolOverload_True_IncludesAll()
    {
        var process = CreateProcess();
        process.Checklists.Add(CreateChecklist(process.Id));

        var view = RentalProcessView.FromEntity(process, includeDetails: true);

        view.Checklists.Should().NotBeNull();
    }

    [Fact]
    public void RentalProcessView_BoolOverload_False_ExcludesAll()
    {
        var process = CreateProcess();

        var view = RentalProcessView.FromEntity(process, includeDetails: false);

        view.Checklists.Should().BeNull();
    }

    [Fact]
    public void RentalProcessView_NullRental_RentalViewIsNull()
    {
        var process = CreateProcess(withRental: false);

        var view = RentalProcessView.FromEntity(process);

        view.Rental.Should().BeNull();
    }

    // ── RentalProcessSummaryView ────────────────────────────────────

    [Fact]
    public void RentalProcessSummaryView_FromEntity_MapsAllProperties()
    {
        var process = CreateProcess();

        var view = RentalProcessSummaryView.FromEntity(process);

        view.Guid.Should().Be(process.Guid);
        view.CurrentStage.Should().Be(process.CurrentStage);
        view.CreatedByKcId.Should().Be(process.CreatedByKcId);
        view.CustomerName.Should().Be(process.Rental!.CustomerName);
        view.CustomerEmail.Should().Be(process.Rental.CustomerEmail);
        view.RequestedStart.Should().Be(process.Rental.RequestedStart);
        view.RequestedEnd.Should().Be(process.Rental.RequestedEnd);
        view.CreatedAt.Should().Be(process.CreatedAt);
        view.UpdatedAt.Should().Be(process.UpdatedAt);
    }

    [Fact]
    public void RentalProcessSummaryView_NullRental_NullableFieldsAreNull()
    {
        var process = CreateProcess(withRental: false);

        var view = RentalProcessSummaryView.FromEntity(process);

        view.CustomerName.Should().BeNull();
        view.CustomerEmail.Should().BeNull();
        view.RequestedStart.Should().BeNull();
        view.RequestedEnd.Should().BeNull();
    }

    // ── ChecklistView ───────────────────────────────────────────────

    [Fact]
    public void ChecklistView_FromEntity_MapsAllProperties()
    {
        var checklist = CreateChecklist(1);

        var view = ChecklistView.FromEntity(checklist);

        view.Guid.Should().Be(checklist.Guid);
        view.ChecklistType.Should().Be(checklist.ChecklistType);
        view.IsSigned.Should().Be(checklist.IsSigned);
        view.CreatedAt.Should().Be(checklist.CreatedAt);
        view.Items.Should().HaveCount(checklist.Items.Count);
    }

    [Fact]
    public void ChecklistItemView_FromEntity_MapsAllProperties()
    {
        var item = new ChecklistItem
        {
            Id = 1,
            Guid = Guid.NewGuid(),
            StockBindingGuid = Guid.NewGuid(),
            DeviceName = "Laptop",
            IsScanned = true,
            ScannedValue = "SN-001",
            ScannedByKcId = "scanner-kc",
            ScannedAt = Now
        };

        var view = ChecklistItemView.FromEntity(item);

        view.Guid.Should().Be(item.Guid);
        view.StockBindingGuid.Should().Be(item.StockBindingGuid);
        view.DeviceName.Should().Be("Laptop");
        view.IsScanned.Should().BeTrue();
        view.ScannedValue.Should().Be("SN-001");
        view.ScannedByKcId.Should().Be("scanner-kc");
        view.ScannedAt.Should().Be(Now);
    }

    // ── RentalActionLogView ─────────────────────────────────────────

    [Fact]
    public void RentalActionLogView_FromEntity_MapsAllProperties()
    {
        var log = new RentalActionLog
        {
            Id = 1,
            Guid = Guid.NewGuid(),
            ProcessInstanceId = 1,
            ActionType = RentalActionType.ApproveRequest,
            PerformedByKcId = "approver-kc",
            StageBefore = RentalStage.Requested,
            StageAfter = RentalStage.Approved,
            Success = true,
            ErrorMessage = null,
            PerformedAt = Now
        };

        var view = RentalActionLogView.FromEntity(log);

        view.Guid.Should().Be(log.Guid);
        view.ActionType.Should().Be(RentalActionType.ApproveRequest);
        view.PerformedByKcId.Should().Be("approver-kc");
        view.StageBefore.Should().Be(RentalStage.Requested);
        view.StageAfter.Should().Be(RentalStage.Approved);
        view.Success.Should().BeTrue();
        view.ErrorMessage.Should().BeNull();
        view.PerformedAt.Should().Be(Now);
    }

    [Fact]
    public void RentalActionLogView_FailedAction_MapsErrorMessage()
    {
        var log = new RentalActionLog
        {
            Id = 2,
            Guid = Guid.NewGuid(),
            ProcessInstanceId = 1,
            ActionType = RentalActionType.CreateRental,
            PerformedByKcId = "actor-kc",
            StageBefore = RentalStage.None,
            StageAfter = RentalStage.None,
            Success = false,
            ErrorMessage = "Validation failed",
            PerformedAt = Now
        };

        var view = RentalActionLogView.FromEntity(log);

        view.Success.Should().BeFalse();
        view.ErrorMessage.Should().Be("Validation failed");
    }

    // ── RentalExtensionView ─────────────────────────────────────────

    [Fact]
    public void RentalExtensionView_FromEntity_MapsAllProperties()
    {
        var extension = CreateExtension(1);

        var view = RentalExtensionView.FromEntity(extension);

        view.Guid.Should().Be(extension.Guid);
        view.NewRequestedEnd.Should().Be(extension.NewRequestedEnd);
        view.OriginalEnd.Should().Be(extension.OriginalEnd);
        view.Reason.Should().Be(extension.Reason);
        view.IsApproved.Should().Be(extension.IsApproved);
        view.ReviewComment.Should().Be(extension.ReviewComment);
        view.RequestedByKcId.Should().Be(extension.RequestedByKcId);
        view.ReviewedByKcId.Should().Be(extension.ReviewedByKcId);
        view.RequestedAt.Should().Be(extension.RequestedAt);
        view.ReviewedAt.Should().Be(extension.ReviewedAt);
    }

    // ── RentalDamageReportView ──────────────────────────────────────

    [Fact]
    public void RentalDamageReportView_FromEntity_MapsAllProperties()
    {
        var report = CreateDamageReport(1);

        var view = RentalDamageReportView.FromEntity(report);

        view.Guid.Should().Be(report.Guid);
        view.StockBindingGuid.Should().Be(report.StockBindingGuid);
        view.Description.Should().Be(report.Description);
        view.Severity.Should().Be(report.Severity);
        view.ReportedByKcId.Should().Be(report.ReportedByKcId);
        view.ReportedAt.Should().Be(report.ReportedAt);
    }

    // ── Factory helpers ─────────────────────────────────────────────

    private static RentalProcessInstance CreateProcess(bool withRental = true)
    {
        var process = new RentalProcessInstance
        {
            Id = 1,
            Guid = Guid.NewGuid(),
            CurrentStage = RentalStage.Approved,
            CreatedByKcId = "creator-kc",
            CreatedAt = Now,
            UpdatedAt = Now
        };

        if (withRental)
        {
            process.RentalId = 1;
            process.Rental = CreateRental();
        }

        return process;
    }

    private static Rental CreateRental() => new()
    {
        Id = 1,
        Uuid = Guid.NewGuid(),
        CustomerKcId = "customer-kc",
        CustomerName = "Jane Doe",
        CustomerEmail = "jane@example.com",
        Purpose = "Conference",
        RequestedStart = Now + Duration.FromDays(1),
        RequestedEnd = Now + Duration.FromDays(7),
        Priority = RentalPriority.NORMAL,
        Notes = "Handle with care",
        CreatedAt = Now,
        UpdatedAt = Now
    };

    private static Checklist CreateChecklist(long processId) => new()
    {
        Id = 10,
        Guid = Guid.NewGuid(),
        ProcessInstanceId = processId,
        ChecklistType = ChecklistType.PICKUP,
        IsSigned = false,
        CreatedAt = Now,
        Items =
        [
            new ChecklistItem
            {
                Id = 100,
                Guid = Guid.NewGuid(),
                StockBindingGuid = Guid.NewGuid(),
                DeviceName = "Laptop-A",
                IsScanned = false
            }
        ]
    };

    private static RentalExtension CreateExtension(long processId) => new()
    {
        Id = 20,
        Guid = Guid.NewGuid(),
        ProcessInstanceId = processId,
        NewRequestedEnd = Now + Duration.FromDays(14),
        OriginalEnd = Now + Duration.FromDays(7),
        Reason = "Need more time",
        IsApproved = null,
        ReviewComment = null,
        RequestedByKcId = "customer-kc",
        ReviewedByKcId = null,
        RequestedAt = Now,
        ReviewedAt = null
    };

    private static RentalDamageReport CreateDamageReport(long processId) => new()
    {
        Id = 30,
        Guid = Guid.NewGuid(),
        ProcessInstanceId = processId,
        StockBindingGuid = Guid.NewGuid(),
        Description = "Screen cracked",
        Severity = DamageSeverity.SEVERE,
        ReportedByKcId = "inspector-kc",
        ReportedAt = Now
    };
}
