using LumenForgeServer.Rentals.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Rentals.Persistence.Configurations;

/// <summary>
/// Entity configuration for <see cref="RentalItem"/>.
/// </summary>
public sealed class RentalItemConfiguration : IEntityTypeConfiguration<RentalItem>
{
    public void Configure(EntityTypeBuilder<RentalItem> builder)
    {
        builder.ToTable("rental_item");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Uuid).IsUnique();

        builder.Property(x => x.Quantity).HasPrecision(18, 3);
        builder.Property(x => x.ConditionNotes).HasMaxLength(4000);
        builder.Property(x => x.ApprovedByUserId).HasMaxLength(128);

        builder.HasOne(x => x.Rental)
            .WithMany(r => r.Items)
            .HasForeignKey(x => x.RentalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Stock)
            .WithMany(s => s.RentalItems)
            .HasForeignKey(x => x.StockId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
