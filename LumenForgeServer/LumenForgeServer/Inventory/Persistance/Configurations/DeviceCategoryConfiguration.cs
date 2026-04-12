using LumenForgeServer.Inventory.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Inventory.Persistance.Configurations;

/// <summary>
/// Entity configuration for <see cref="DeviceCategory"/>.
/// </summary>
public sealed class DeviceCategoryConfiguration : IEntityTypeConfiguration<DeviceCategory>
{
    /// <summary>
    /// Executes the configure operation.
    /// </summary>
    /// <remarks>Potential side effects: may modify state as part of this operation.</remarks>
    /// <param name="builder">Input value used by this operation.</param>
    public void Configure(EntityTypeBuilder<DeviceCategory> builder)
    {
        builder.ToTable("device_category");

        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Device)
            .WithMany(d => d.DeviceCategories)
            .HasForeignKey(x => x.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Category)
            .WithMany(c => c.DeviceCategories)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.DeviceId, x.CategoryId }).IsUnique();
    }
}
