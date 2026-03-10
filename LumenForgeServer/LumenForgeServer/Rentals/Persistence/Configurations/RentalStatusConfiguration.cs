using LumenForgeServer.Rentals.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Rentals.Persistence.Configurations;

/// <summary>
/// Entity configuration for <see cref="RentalStatus"/>.
/// </summary>
public sealed class RentalStatusConfiguration : IEntityTypeConfiguration<RentalStatus>
{
    public void Configure(EntityTypeBuilder<RentalStatus> builder)
    {
        builder.ToTable("rental_status");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.HasIndex(x => x.Name).IsUnique();
    }
}
