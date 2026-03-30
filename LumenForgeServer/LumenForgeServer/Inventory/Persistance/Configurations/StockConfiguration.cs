using LumenForgeServer.Inventory.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Inventory.Persistance.Configurations;

/// <summary>
/// Entity configuration for <see cref="StockBinding"/>.
/// </summary>
public sealed class StockConfiguration : IEntityTypeConfiguration<StockBinding>
{
    /// <summary>
    /// Executes the configure operation.
    /// </summary>
    /// <remarks>Potential side effects: may modify state as part of this operation.</remarks>
    /// <param name="builder">Input value used by this operation.</param>
    public void Configure(EntityTypeBuilder<StockBinding> builder)
    {
        builder.ToTable("stock");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Guid).IsUnique();
        builder.HasIndex(x => x.DeviceId);
        builder.HasIndex(x => x.OwnerProcessGuid);

        builder.Property(x => x.BindingType).IsRequired();
        builder.Property(x => x.ReservedAmount).IsRequired();
        builder.Property(x => x.Start).IsRequired();
        builder.Property(x => x.End).IsRequired();
    }
}
