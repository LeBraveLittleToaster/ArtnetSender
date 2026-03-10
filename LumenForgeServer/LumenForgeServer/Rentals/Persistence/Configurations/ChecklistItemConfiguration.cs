using LumenForgeServer.Rentals.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Rentals.Persistence.Configurations;

/// <summary>
/// Entity configuration for <see cref="ChecklistItem"/>.
/// </summary>
public sealed class ChecklistItemConfiguration : IEntityTypeConfiguration<ChecklistItem>
{
    public void Configure(EntityTypeBuilder<ChecklistItem> builder)
    {
        builder.ToTable("checklist_item");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Uuid).IsUnique();

        builder.Property(x => x.QuantityChecked).HasPrecision(18, 3);
        builder.Property(x => x.DamagedQuantity).HasPrecision(18, 3);

        builder.Property(x => x.ConditionNotes).HasMaxLength(4000);
        builder.Property(x => x.DamageSummary).HasMaxLength(2000);
        builder.Property(x => x.DamageDescription).HasMaxLength(4000);

        builder.HasOne(x => x.Checklist)
            .WithMany(c => c.Items)
            .HasForeignKey(x => x.ChecklistId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.RentalItem)
            .WithMany(ri => ri.ChecklistItems)
            .HasForeignKey(x => x.RentalItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ChecklistId, x.RentalItemId }).IsUnique();
    }
}
