using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Domain.Actions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Rentals.Persistence.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="RentalAction"/> TPT hierarchy.
/// The base table holds shared audit columns; each concrete subtype maps to its own table
/// with action-specific companion columns.
/// </summary>
public sealed class RentalActionConfiguration : IEntityTypeConfiguration<RentalAction>
{
    public void Configure(EntityTypeBuilder<RentalAction> builder)
    {
        builder.UseTptMappingStrategy();
        builder.ToTable("rental_action");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Uuid).IsUnique();

        builder.Property(x => x.ActionType)
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.PerformedByUserId).HasMaxLength(128);

        builder.HasOne(x => x.Rental)
            .WithMany(r => r.Actions)
            .HasForeignKey(x => x.RentalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.RentalId, x.ExecutedAt });
    }
}

// ---------------------------------------------------------------------------
// Pure status-transition actions (no extra columns)
// ---------------------------------------------------------------------------

public sealed class ApproveRequestActionConfiguration : IEntityTypeConfiguration<ApproveRequestAction>
{
    public void Configure(EntityTypeBuilder<ApproveRequestAction> b)
        => b.ToTable("rental_action_approve_request");
}

public sealed class RecordPickupActionConfiguration : IEntityTypeConfiguration<RecordPickupAction>
{
    public void Configure(EntityTypeBuilder<RecordPickupAction> b)
        => b.ToTable("rental_action_record_pickup");
}

public sealed class RecordReturnActionConfiguration : IEntityTypeConfiguration<RecordReturnAction>
{
    public void Configure(EntityTypeBuilder<RecordReturnAction> b)
        => b.ToTable("rental_action_record_return");
}

public sealed class CompleteRentalActionConfiguration : IEntityTypeConfiguration<CompleteRentalAction>
{
    public void Configure(EntityTypeBuilder<CompleteRentalAction> b)
        => b.ToTable("rental_action_complete_rental");
}

// ---------------------------------------------------------------------------
// Item actions (no extra columns)
// ---------------------------------------------------------------------------

public sealed class AssignItemsActionConfiguration : IEntityTypeConfiguration<AssignItemsAction>
{
    public void Configure(EntityTypeBuilder<AssignItemsAction> b)
        => b.ToTable("rental_action_assign_items");
}

public sealed class RemoveItemsActionConfiguration : IEntityTypeConfiguration<RemoveItemsAction>
{
    public void Configure(EntityTypeBuilder<RemoveItemsAction> b)
        => b.ToTable("rental_action_remove_items");
}

public sealed class ApproveItemsActionConfiguration : IEntityTypeConfiguration<ApproveItemsAction>
{
    public void Configure(EntityTypeBuilder<ApproveItemsAction> b)
        => b.ToTable("rental_action_approve_items");
}

public sealed class RejectItemsActionConfiguration : IEntityTypeConfiguration<RejectItemsAction>
{
    public void Configure(EntityTypeBuilder<RejectItemsAction> b)
        => b.ToTable("rental_action_reject_items");
}

// ---------------------------------------------------------------------------
// Side-effect actions (no extra columns)
// ---------------------------------------------------------------------------

public sealed class RecordDamagesActionConfiguration : IEntityTypeConfiguration<RecordDamagesAction>
{
    public void Configure(EntityTypeBuilder<RecordDamagesAction> b)
        => b.ToTable("rental_action_record_damages");
}

public sealed class CreateMaintenanceJobsActionConfiguration : IEntityTypeConfiguration<CreateMaintenanceJobsAction>
{
    public void Configure(EntityTypeBuilder<CreateMaintenanceJobsAction> b)
        => b.ToTable("rental_action_create_maintenance_jobs");
}

public sealed class GenerateInvoiceActionConfiguration : IEntityTypeConfiguration<GenerateInvoiceAction>
{
    public void Configure(EntityTypeBuilder<GenerateInvoiceAction> b)
        => b.ToTable("rental_action_generate_invoice");
}

public sealed class RecordPaymentActionConfiguration : IEntityTypeConfiguration<RecordPaymentAction>
{
    public void Configure(EntityTypeBuilder<RecordPaymentAction> b)
        => b.ToTable("rental_action_record_payment");
}

public sealed class GenerateReportActionConfiguration : IEntityTypeConfiguration<GenerateReportAction>
{
    public void Configure(EntityTypeBuilder<GenerateReportAction> b)
        => b.ToTable("rental_action_generate_report");
}

// ---------------------------------------------------------------------------
// Reason-bearing actions
// ---------------------------------------------------------------------------

public sealed class RejectRequestActionConfiguration : IEntityTypeConfiguration<RejectRequestAction>
{
    public void Configure(EntityTypeBuilder<RejectRequestAction> b)
    {
        b.ToTable("rental_action_reject_request");
        b.Property(x => x.Reason).IsRequired().HasMaxLength(2000);
    }
}

public sealed class CancelRentalActionConfiguration : IEntityTypeConfiguration<CancelRentalAction>
{
    public void Configure(EntityTypeBuilder<CancelRentalAction> b)
    {
        b.ToTable("rental_action_cancel_rental");
        b.Property(x => x.Reason).HasMaxLength(2000);
    }
}

public sealed class ScrapRentalActionConfiguration : IEntityTypeConfiguration<ScrapRentalAction>
{
    public void Configure(EntityTypeBuilder<ScrapRentalAction> b)
    {
        b.ToTable("rental_action_scrap_rental");
        b.Property(x => x.Reason).HasMaxLength(2000);
    }
}

// ---------------------------------------------------------------------------
// Checklist-linked actions
// ---------------------------------------------------------------------------

public sealed class GenerateChecklistActionConfiguration : IEntityTypeConfiguration<GenerateChecklistAction>
{
    public void Configure(EntityTypeBuilder<GenerateChecklistAction> b)
    {
        b.ToTable("rental_action_generate_checklist");
        b.Property(x => x.ChecklistType).HasConversion<string>().HasMaxLength(16).IsRequired();
        b.HasOne(x => x.Checklist).WithMany().HasForeignKey(x => x.ChecklistId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ScanChecklistActionConfiguration : IEntityTypeConfiguration<ScanChecklistAction>
{
    public void Configure(EntityTypeBuilder<ScanChecklistAction> b)
    {
        b.ToTable("rental_action_scan_checklist");
        b.HasOne(x => x.Checklist).WithMany().HasForeignKey(x => x.ChecklistId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class SignChecklistActionConfiguration : IEntityTypeConfiguration<SignChecklistAction>
{
    public void Configure(EntityTypeBuilder<SignChecklistAction> b)
    {
        b.ToTable("rental_action_sign_checklist");
        b.Property(x => x.Notes).HasMaxLength(2000);
        b.HasOne(x => x.Checklist).WithMany().HasForeignKey(x => x.ChecklistId).OnDelete(DeleteBehavior.Cascade);
    }
}

// ---------------------------------------------------------------------------
// Extension-linked actions
// ---------------------------------------------------------------------------

public sealed class RequestExtensionActionConfiguration : IEntityTypeConfiguration<RequestExtensionAction>
{
    public void Configure(EntityTypeBuilder<RequestExtensionAction> b)
    {
        b.ToTable("rental_action_request_extension");
        b.HasOne(x => x.Extension).WithMany().HasForeignKey(x => x.ExtensionId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ApproveExtensionActionConfiguration : IEntityTypeConfiguration<ApproveExtensionAction>
{
    public void Configure(EntityTypeBuilder<ApproveExtensionAction> b)
    {
        b.ToTable("rental_action_approve_extension");
        b.HasOne(x => x.Extension).WithMany().HasForeignKey(x => x.ExtensionId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class RejectExtensionActionConfiguration : IEntityTypeConfiguration<RejectExtensionAction>
{
    public void Configure(EntityTypeBuilder<RejectExtensionAction> b)
    {
        b.ToTable("rental_action_reject_extension");
        b.Property(x => x.Reason).HasMaxLength(2000);
        b.HasOne(x => x.Extension).WithMany().HasForeignKey(x => x.ExtensionId).OnDelete(DeleteBehavior.Cascade);
    }
}
