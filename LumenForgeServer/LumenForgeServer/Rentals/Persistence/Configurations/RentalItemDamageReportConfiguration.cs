using LumenForgeServer.Maintenance.Domain;
using LumenForgeServer.Rentals.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Rentals.Persistence.Configurations;

/// <summary>
/// Entity configuration for <see cref="RentalItemDamageReport"/>.
/// </summary>
public sealed class RentalItemDamageReportConfiguration : IEntityTypeConfiguration<RentalItemDamageReport>
{
    public void Configure(EntityTypeBuilder<RentalItemDamageReport> builder)
    {
        builder.ToTable("rental_item_damage_report");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Uuid).IsUnique();

        builder.Property(x => x.Severity).HasConversion<string>().HasMaxLength(16).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.PhotoUrl).HasMaxLength(2000);

        builder.Property(x => x.EstimatedRepairCost).HasPrecision(18, 4);
        builder.Property(x => x.ActualRepairCost).HasPrecision(18, 4);

        builder.Property(x => x.ReportedByUserId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ResolvedByUserId).HasMaxLength(128);
        builder.Property(x => x.ResolutionNotes).HasMaxLength(4000);

        builder.HasOne(x => x.RentalItem)
            .WithMany(ri => ri.DamageReports)
            .HasForeignKey(x => x.RentalItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ChecklistItem)
            .WithMany()
            .HasForeignKey(x => x.ChecklistItemId)
            .OnDelete(DeleteBehavior.SetNull);

        // Nullable FK: damage report exists independently; job is created after the fact
        builder.HasOne(x => x.MaintenanceJob)
            .WithMany()
            .HasForeignKey(x => x.MaintenanceJobId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.RentalItemId);
        builder.HasIndex(x => x.MaintenanceJobId);
    }
}
