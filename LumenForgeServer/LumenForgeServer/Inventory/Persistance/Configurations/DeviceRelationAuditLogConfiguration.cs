using LumenForgeServer.Inventory.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Inventory.Persistance.Configurations;

public sealed class DeviceRelationAuditLogConfiguration : IEntityTypeConfiguration<DeviceRelationAuditLog>
{
    public void Configure(EntityTypeBuilder<DeviceRelationAuditLog> builder)
    {
        builder.ToTable("device_relation_audit_log");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Guid).IsUnique();
        builder.HasIndex(x => x.RelationGuid);
        builder.HasIndex(x => x.ParentDeviceId);
        builder.HasIndex(x => x.ChildDeviceId);

        builder.Property(x => x.RelationGuid).IsRequired();
        builder.Property(x => x.ContainedAmount).IsRequired();
        builder.Property(x => x.RelationType).IsRequired();
        builder.Property(x => x.Action).IsRequired();
        builder.Property(x => x.OccurredAt).IsRequired();
    }
}
