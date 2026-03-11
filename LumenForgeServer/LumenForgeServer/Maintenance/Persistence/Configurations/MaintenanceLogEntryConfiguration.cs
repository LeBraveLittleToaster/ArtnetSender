using LumenForgeServer.Maintenance.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Maintenance.Persistence.Configurations;

/// <summary>
/// Entity configuration for <see cref="MaintenanceLogEntry"/>.
/// </summary>
public sealed class MaintenanceLogEntryConfiguration : IEntityTypeConfiguration<MaintenanceLogEntry>
{
    public void Configure(EntityTypeBuilder<MaintenanceLogEntry> builder)
    {
        builder.ToTable("maintenance_log_entry");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Guid).IsUnique();

        builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.StatusBefore).IsRequired();
        builder.Property(x => x.StatusAfter).IsRequired();
    }
}
