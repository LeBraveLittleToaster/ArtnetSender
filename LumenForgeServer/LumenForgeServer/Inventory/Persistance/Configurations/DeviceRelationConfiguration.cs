using LumenForgeServer.Inventory.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Inventory.Persistance.Configurations;

public sealed class DeviceRelationConfiguration : IEntityTypeConfiguration<DeviceRelation>
{
    public void Configure(EntityTypeBuilder<DeviceRelation> builder)
    {
        builder.ToTable("device_relation");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Guid).IsUnique();
        builder.HasIndex(x => new { x.ParentDeviceId, x.ChildDeviceId }).IsUnique();

        builder.Property(x => x.ContainedAmount).IsRequired();
        builder.Property(x => x.RelationType).IsRequired();

        builder.HasOne(x => x.ParentDevice)
            .WithMany(d => d.ChildDeviceRelations)
            .HasForeignKey(x => x.ParentDeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ChildDevice)
            .WithMany(d => d.ParentDeviceRelations)
            .HasForeignKey(x => x.ChildDeviceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
