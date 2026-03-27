using NodaTime;

namespace LumenForgeServer.Inventory.Domain;

public class DeviceRelationAuditLog
{
    public long Id { get; set; }
    public Guid Guid { get; set; }

    public Guid RelationGuid { get; set; }

    public long ParentDeviceId { get; set; }
    public long ChildDeviceId { get; set; }

    public long ContainedAmount { get; set; }
    public DeviceRelationType RelationType { get; set; }

    public DeviceRelationAuditAction Action { get; set; }
    public Instant OccurredAt { get; set; }
}
