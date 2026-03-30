using LumenForgeServer.Inventory.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Inventory.Persistance.Configurations;

/// <summary>
/// Entity configuration for <see cref="Device"/>.
/// </summary>
public sealed class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    /// <summary>
    /// Executes the configure operation.
    /// </summary>
    /// <remarks>Potential side effects: may modify state as part of this operation.</remarks>
    /// <param name="builder">Input value used by this operation.</param>
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.ToTable("device");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.SerialNumber).HasMaxLength(256).IsRequired();
        builder.HasIndex(x => x.Guid).IsUnique();
        builder.HasIndex(x => x.SerialNumber).IsUnique();

        builder.Property(x => x.DeviceName).HasMaxLength(512);
        builder.Property(x => x.DeviceDescription).HasMaxLength(4000);
        builder.Property(x => x.PhotoUrl).HasMaxLength(2000);

        builder.Property(x => x.PurchasePrice).HasPrecision(18, 2);
        builder.Property(x => x.PurchaseDate);

        builder.HasOne(x => x.Vendor)
            .WithMany(v => v.Devices)
            .HasForeignKey(x => x.VendorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.MaintenanceStatus)
            .WithMany(ms => ms.Devices)
            .HasForeignKey(x => x.MaintenanceStatusId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasMany(x => x.StockBindings)
            .WithOne(s => s.Device)
            .HasForeignKey(s => s.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
