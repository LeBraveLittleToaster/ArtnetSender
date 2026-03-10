using LumenForgeServer.Maintenance.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Maintenance.Persistence.Configurations;

/// <summary>
/// Entity configuration for <see cref="MaintenanceBacklogStatus"/>.
/// </summary>
public sealed class MaintenanceBacklogStatusConfiguration : IEntityTypeConfiguration<MaintenanceBacklogStatus>
{
    public void Configure(EntityTypeBuilder<MaintenanceBacklogStatus> builder)
    {
        builder.ToTable("maintenance_backlog_status");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.HasIndex(x => x.Name).IsUnique();
    }
}
