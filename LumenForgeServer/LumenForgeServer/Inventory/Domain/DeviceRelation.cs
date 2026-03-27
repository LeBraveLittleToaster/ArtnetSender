using NodaTime;

namespace LumenForgeServer.Inventory.Domain;

public class DeviceRelation
{
    public long Id { get; set; }
    public Guid Guid { get; set; }

    public long ParentDeviceId { get; set; }
    public Device ParentDevice { get; set; } = null!;

    public long ChildDeviceId { get; set; }
    public Device ChildDevice { get; set; } = null!;

    public long ContainedAmount { get; set; }
    public DeviceRelationType RelationType { get; set; }

    public Instant CreatedAt { get; set; }
    public Instant UpdatedAt { get; set; }
}
