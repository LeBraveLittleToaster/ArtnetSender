using LumenForgeServer.Rentals.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Rentals.Persistence.Configurations;

/// <summary>
/// Entity configuration for <see cref="RentalExtension"/>.
/// </summary>
public sealed class RentalExtensionConfiguration : IEntityTypeConfiguration<RentalExtension>
{
    /// <summary>
    /// Executes the configure operation.
    /// Core concept: maps application requests to persistence queries and materializes domain data.
    /// </summary>
    /// <remarks>Potential side effects: may execute database writes or update tracked entity state in the current DbContext.</remarks>
    /// <param name="builder">Input value used by this operation.</param>
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
