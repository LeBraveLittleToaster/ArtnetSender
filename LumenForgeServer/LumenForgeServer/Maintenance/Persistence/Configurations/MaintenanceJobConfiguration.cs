using LumenForgeServer.Maintenance.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Maintenance.Persistence.Configurations;

/// <summary>
/// Entity configuration for <see cref="MaintenanceJob"/>.
/// </summary>
public sealed class MaintenanceJobConfiguration : IEntityTypeConfiguration<MaintenanceJob>
{
    /// <summary>
    /// Executes the configure operation.
    /// Core concept: maps application requests to persistence queries and materializes domain data.
    /// </summary>
    /// <remarks>Potential side effects: may execute database writes or update tracked entity state in the current DbContext.</remarks>
    /// <param name="builder">Numeric input used by this operation.</param>
    public void Configure(EntityTypeBuilder<MaintenanceJob> builder)
    {
        builder.ToTable("maintenance_job");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Guid).IsUnique();

        builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.CreatedByUserKcId).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Status).IsRequired();

        builder.HasOne(x => x.RelatedToRental)
            .WithMany()
            .HasForeignKey(x => x.RelatedToRentalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Tasks)
            .WithOne(x => x.MaintenanceJob)
            .HasForeignKey(x => x.MaintenanceJobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.AffectedDevices)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "maintenance_job_device",
                r => r.HasOne<LumenForgeServer.Inventory.Domain.Device>().WithMany().HasForeignKey("device_id").OnDelete(DeleteBehavior.Cascade),
                l => l.HasOne<MaintenanceJob>().WithMany().HasForeignKey("maintenance_job_id").OnDelete(DeleteBehavior.Cascade),
                j =>
                {
                    j.ToTable("maintenance_job_device");
                    j.HasKey("maintenance_job_id", "device_id");
                });

        builder.HasMany(x => x.RelatedJobs)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "maintenance_job_relation",
                r => r.HasOne<MaintenanceJob>().WithMany().HasForeignKey("related_job_id").OnDelete(DeleteBehavior.Restrict),
                l => l.HasOne<MaintenanceJob>().WithMany().HasForeignKey("maintenance_job_id").OnDelete(DeleteBehavior.Cascade),
                j =>
                {
                    j.ToTable("maintenance_job_relation");
                    j.HasKey("maintenance_job_id", "related_job_id");
                });
    }
}
