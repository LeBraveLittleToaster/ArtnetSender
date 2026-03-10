using LumenForgeServer.Inventory.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Inventory.Persistance.Configurations;

/// <summary>
/// Entity configuration for <see cref="DeviceParameter"/>.
/// </summary>
public sealed class DeviceParameterConfiguration : IEntityTypeConfiguration<DeviceParameter>
{
    public void Configure(EntityTypeBuilder<DeviceParameter> builder)
    {
        builder.ToTable("device_parameter");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.ParamKey).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Value).HasMaxLength(4000).IsRequired();

        builder.HasOne(x => x.Device)
            .WithMany(d => d.Parameters)
            .HasForeignKey(x => x.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.DeviceId, x.ParamKey }).IsUnique();
    }
}
