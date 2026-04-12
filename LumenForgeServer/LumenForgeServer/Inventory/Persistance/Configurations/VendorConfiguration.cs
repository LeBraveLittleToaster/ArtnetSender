using LumenForgeServer.Inventory.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Inventory.Persistance.Configurations;

/// <summary>
/// Entity configuration for <see cref="Vendor"/>.
/// </summary>
public sealed class VendorConfiguration : IEntityTypeConfiguration<Vendor>
{
    /// <summary>
    /// Executes the configure operation.
    /// </summary>
    /// <remarks>Potential side effects: may modify state as part of this operation.</remarks>
    /// <param name="builder">Input value used by this operation.</param>
    public void Configure(EntityTypeBuilder<Vendor> builder)
    {
        builder.ToTable("vendor");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
        builder.HasIndex(x => x.Guid).IsUnique();
        builder.HasIndex(x => x.Name).IsUnique();
    }
}
