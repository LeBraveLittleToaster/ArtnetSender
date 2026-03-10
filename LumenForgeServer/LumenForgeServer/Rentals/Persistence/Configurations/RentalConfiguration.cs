using LumenForgeServer.Rentals.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Rentals.Persistence.Configurations;

/// <summary>
/// Entity configuration for <see cref="Rental"/>.
/// </summary>
public sealed class RentalConfiguration : IEntityTypeConfiguration<Rental>
{
    public void Configure(EntityTypeBuilder<Rental> builder)
    {
        builder.ToTable("rental");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Uuid).IsUnique();

        builder.Property(x => x.CustomerUserId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.AssignedByUserId).HasMaxLength(128);
        builder.Property(x => x.PickupProcessedByUserId).HasMaxLength(128);
        builder.Property(x => x.DropoffProcessedByUserId).HasMaxLength(128);
        builder.Property(x => x.CompletedByUserId).HasMaxLength(128);
        builder.Property(x => x.ScrappedByUserId).HasMaxLength(128);

        builder.HasIndex(x => x.CustomerUserId);

        builder.Property(x => x.RequestTitle).HasMaxLength(512);
        builder.Property(x => x.RequestDescription).HasMaxLength(4000);

        builder.HasOne(x => x.RentalStatus)
            .WithMany(s => s.Rentals)
            .HasForeignKey(x => x.RentalStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.RentalReport)
            .WithOne(rr => rr.Rental)
            .HasForeignKey<RentalReport>(rr => rr.RentalId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
