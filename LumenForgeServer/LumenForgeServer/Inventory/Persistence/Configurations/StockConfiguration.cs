using LumenForgeServer.Inventory.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Inventory.Persistence.Configurations;

/// <summary>
/// Entity configuration for <see cref="Stock"/>.
/// </summary>
public sealed class StockConfiguration : IEntityTypeConfiguration<Stock>
{
    public void Configure(EntityTypeBuilder<Stock> builder)
    {
        builder.ToTable("stock");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.HasIndex(x => x.DeviceId).IsUnique();

        builder.Property(x => x.StockCount).HasPrecision(18, 3);
        builder.Property(x => x.UnitStockType).HasConversion<string>().HasMaxLength(32).IsRequired();
    }
}
