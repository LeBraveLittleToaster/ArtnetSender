using NodaTime;

namespace LumenForgeServer.Inventory.Domain;

public class StockBinding
{
    public long Id { get; set; }
    public Guid Guid { get; set; }
    public long DeviceId { get; set; }
    public Device Device { get; set; } = null!;
    public BindingType BindingType { get; set; }
    public Instant CreatedAt{ get; set; }
    public Instant Start { get; set; }
    public Instant End { get; set; }
}

