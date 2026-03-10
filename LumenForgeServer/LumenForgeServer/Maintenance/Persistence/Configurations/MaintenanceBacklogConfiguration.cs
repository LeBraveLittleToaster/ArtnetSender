using LumenForgeServer.Maintenance.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Maintenance.Persistence.Configurations;

/// <summary>
/// Entity configuration for <see cref="MaintenanceBacklog"/>.
/// </summary>
public sealed class MaintenanceBacklogConfiguration : IEntityTypeConfiguration<MaintenanceBacklog>
{
    public void Configure(EntityTypeBuilder<MaintenanceBacklog> builder)
    {
        builder.ToTable("maintenance_backlog");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Uuid).IsUnique();

        builder.Property(x => x.QuantityAffected).HasPrecision(18, 3);
        builder.Property(x => x.IssueSummary).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.IssueDescription).HasMaxLength(4000);

        builder.HasOne(x => x.RentalItem)
            .WithMany(ri => ri.MaintenanceBacklogs)
            .HasForeignKey(x => x.RentalItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ChecklistItem)
            .WithMany(ci => ci.MaintenanceBacklogs)
            .HasForeignKey(x => x.ChecklistItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Device)
            .WithMany()
            .HasForeignKey(x => x.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.MaintenanceBacklogStatus)
            .WithMany(s => s.MaintenanceBacklogs)
            .HasForeignKey(x => x.MaintenanceBacklogStatusId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
