using LumenForgeServer.Inventory.Domain;
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

        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();

        builder.Property(x => x.QuantityRequested).HasPrecision(18, 3).IsRequired();
        builder.Property(x => x.QuantityApproved).HasPrecision(18, 3);
        builder.Property(x => x.QuantityPickedUp).HasPrecision(18, 3);
        builder.Property(x => x.QuantityReturned).HasPrecision(18, 3);
        builder.Property(x => x.QuantityDamaged).HasPrecision(18, 3);
        builder.Property(x => x.QuantityLost).HasPrecision(18, 3);

        builder.Property(x => x.ApprovedByUserId).HasMaxLength(128);
        builder.Property(x => x.PickupProcessedByUserId).HasMaxLength(128);
        builder.Property(x => x.ReturnProcessedByUserId).HasMaxLength(128);

        builder.Property(x => x.RejectionReason).HasMaxLength(2000);
        builder.Property(x => x.ConditionNotes).HasMaxLength(4000);
        builder.Property(x => x.PickupNotes).HasMaxLength(4000);
        builder.Property(x => x.ReturnNotes).HasMaxLength(4000);

        builder.Property(x => x.DailyRate).HasPrecision(18, 4);
        builder.Property(x => x.DepositAmount).HasPrecision(18, 4);

        builder.HasOne(x => x.Rental)
            .WithMany(r => r.Items)
            .HasForeignKey(x => x.RentalId)
            .OnDelete(DeleteBehavior.Cascade);

        // One StockBinding per physical unit; binding must not be deleted while referenced
        builder.HasMany(x => x.StockBindings)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "rental_item_stock_binding",
                r => r.HasOne<StockBinding>().WithMany()
                    .HasForeignKey("stock_binding_id")
                    .OnDelete(DeleteBehavior.Restrict),
                l => l.HasOne<RentalItem>().WithMany()
                    .HasForeignKey("rental_item_id")
                    .OnDelete(DeleteBehavior.Cascade),
                j =>
                {
                    j.ToTable("rental_item_stock_binding");
                    j.HasKey("rental_item_id", "stock_binding_id");
                });
    }
}
