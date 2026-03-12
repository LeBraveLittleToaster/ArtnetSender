using LumenForgeServer.Rentals.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Rentals.Persistence.Configurations;

/// <summary>
/// Entity configuration for <see cref="RentalEvent"/>.
/// </summary>
public sealed class RentalEventConfiguration : IEntityTypeConfiguration<RentalEvent>
{
    public void Configure(EntityTypeBuilder<RentalEvent> builder)
    {
        builder.ToTable("rental_event");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Uuid).IsUnique();

        builder.Property(x => x.EventType).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.PerformedByUserId).HasMaxLength(128);

        builder.HasOne(x => x.Rental)
            .WithMany(r => r.Events)
            .HasForeignKey(x => x.RentalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.RentalItem)
            .WithMany()
            .HasForeignKey(x => x.RentalItemId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Answers)
            .WithOne(a => a.RentalEvent)
            .HasForeignKey(a => a.RentalEventId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.RentalId, x.OccurredAt });
    }
}
