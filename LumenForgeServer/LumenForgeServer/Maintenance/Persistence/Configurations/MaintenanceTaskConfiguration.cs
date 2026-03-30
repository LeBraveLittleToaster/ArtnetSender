using LumenForgeServer.Maintenance.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Maintenance.Persistence.Configurations;

/// <summary>
/// Entity configuration for <see cref="MaintenanceTask"/>.
/// </summary>
public sealed class MaintenanceTaskConfiguration : IEntityTypeConfiguration<MaintenanceTask>
{
    /// <summary>
    /// Executes the configure operation.
    /// Core concept: maps application requests to persistence queries and materializes domain data.
    /// </summary>
    /// <remarks>Potential side effects: may execute database writes or update tracked entity state in the current DbContext.</remarks>
    /// <param name="builder">Numeric input used by this operation.</param>
    public void Configure(EntityTypeBuilder<MaintenanceTask> builder)
    {
        builder.ToTable("maintenance_task");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Guid).IsUnique();

        builder.Property(x => x.Description).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.AssignedToUserKcId).HasMaxLength(256);
        builder.Property(x => x.Status).IsRequired();

        builder.HasMany(x => x.Log)
            .WithOne(x => x.MaintenanceTask)
            .HasForeignKey(x => x.MaintenanceTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.AffectedDevices)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "maintenance_task_device",
                r => r.HasOne<LumenForgeServer.Inventory.Domain.Device>().WithMany().HasForeignKey("device_id").OnDelete(DeleteBehavior.Cascade),
                l => l.HasOne<MaintenanceTask>().WithMany().HasForeignKey("maintenance_task_id").OnDelete(DeleteBehavior.Cascade),
                j =>
                {
                    j.ToTable("maintenance_task_device");
                    j.HasKey("maintenance_task_id", "device_id");
                });
    }
}
