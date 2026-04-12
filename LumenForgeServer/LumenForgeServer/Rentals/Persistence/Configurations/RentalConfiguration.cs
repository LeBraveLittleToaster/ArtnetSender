using LumenForgeServer.Rentals.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Rentals.Persistence.Configurations;

/// <summary>
/// Entity configuration for <see cref="Rental"/>.
/// </summary>
public sealed class RentalConfiguration : IEntityTypeConfiguration<Rental>
{
    /// <summary>
    /// Executes the configure operation.
    /// Core concept: maps application requests to persistence queries and materializes domain data.
    /// </summary>
    /// <remarks>Potential side effects: may execute database writes or update tracked entity state in the current DbContext.</remarks>
    /// <param name="builder">Input value used by this operation.</param>
    public void Configure(EntityTypeBuilder<Rental> builder)
    {
        builder.ToTable("rental");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Uuid).IsUnique();

        builder.Property(x => x.CustomerKcId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.CustomerName).HasMaxLength(256);
        builder.Property(x => x.CustomerEmail).HasMaxLength(320);
        builder.Property(x => x.Purpose).HasMaxLength(2000);
        builder.Property(x => x.Notes).HasMaxLength(4000);
        builder.Property(x => x.GroupGuid);
        builder.Property(x => x.Priority).IsRequired();

        builder.HasIndex(x => x.CustomerKcId);
        builder.HasIndex(x => x.GroupGuid);
        builder.HasIndex(x => x.CreatedAt);

    }
}
