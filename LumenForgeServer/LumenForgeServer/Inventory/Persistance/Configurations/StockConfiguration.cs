using LumenForgeServer.Inventory.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Inventory.Persistance.Configurations;

/// <summary>
/// Entity configuration for <see cref="Stock"/>.
/// </summary>
public sealed class StockConfiguration : IEntityTypeConfiguration<StockBinding>
{
    public void Configure(EntityTypeBuilder<StockBinding> builder)
    {
        builder.ToTable("stock");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Guid).IsUnique();
        builder.HasIndex(x => x.DeviceId).IsUnique();

        builder.Property(x => x.BindingType).HasPrecision(18, 3);
        builder.Property(x => x.Start).IsRequired();
        builder.Property(x => x.End).IsRequired();
    }
}
