using LumenForgeServer.Rentals.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Rentals.Persistence.Configurations;

/// <summary>
/// Entity configuration for <see cref="RentalExtension"/>.
/// </summary>
public sealed class RentalExtensionConfiguration : IEntityTypeConfiguration<RentalExtension>
{
    public void Configure(EntityTypeBuilder<RentalExtension> builder)
    {
        builder.ToTable("rental_extension");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Uuid).IsUnique();

        builder.Property(x => x.Reason).HasMaxLength(2000);
        builder.Property(x => x.RequestedByUserId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ApprovedByUserId).HasMaxLength(128);
        builder.Property(x => x.RejectionReason).HasMaxLength(2000);

        builder.HasOne(x => x.Rental)
            .WithMany(r => r.Extensions)
            .HasForeignKey(x => x.RentalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.RentalId);
    }
}
