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
        builder.HasIndex(x => x.Guid).IsUnique();

        builder.Property(x => x.Reason).HasMaxLength(2000);
        builder.Property(x => x.ReviewComment).HasMaxLength(2000);
        builder.Property(x => x.RequestedByKcId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ReviewedByKcId).HasMaxLength(128);

        builder.HasIndex(x => x.ProcessInstanceId);
        builder.HasIndex(x => x.IsApproved);
    }
}
