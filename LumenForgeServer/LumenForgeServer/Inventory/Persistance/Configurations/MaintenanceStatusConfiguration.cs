using LumenForgeServer.Inventory.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Inventory.Persistance.Configurations;

/// <summary>
/// Entity configuration for <see cref="MaintenanceStatus"/>.
/// </summary>
public sealed class MaintenanceStatusConfiguration : IEntityTypeConfiguration<MaintenanceStatus>
{
    /// <summary>
    /// Executes the configure operation.
    /// </summary>
    /// <remarks>Potential side effects: may modify state as part of this operation.</remarks>
    /// <param name="builder">Numeric input used by this operation.</param>
    public void Configure(EntityTypeBuilder<MaintenanceStatus> builder)
    {
        builder.ToTable("maintenance_status");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.HasIndex(x => x.Name).IsUnique();
    }
}
