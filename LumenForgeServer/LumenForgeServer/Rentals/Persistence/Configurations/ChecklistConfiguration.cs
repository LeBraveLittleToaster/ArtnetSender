using LumenForgeServer.Rentals.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Rentals.Persistence.Configurations;

/// <summary>
/// Entity configuration for <see cref="Checklist"/>.
/// </summary>
public sealed class ChecklistConfiguration : IEntityTypeConfiguration<Checklist>
{
    /// <summary>
    /// Executes the configure operation.
    /// Core concept: maps application requests to persistence queries and materializes domain data.
    /// </summary>
    /// <remarks>Potential side effects: may execute database writes or update tracked entity state in the current DbContext.</remarks>
    /// <param name="builder">Input value used by this operation.</param>
    public void Configure(EntityTypeBuilder<Checklist> builder)
    {
        builder.ToTable("checklist");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Guid).IsUnique();

        builder.Property(x => x.ChecklistType).IsRequired();
        builder.Property(x => x.SignedByKcId).HasMaxLength(128);
        builder.Property(x => x.SignatureData).HasMaxLength(8000);

        builder.HasIndex(x => x.ProcessInstanceId);

        builder.HasMany(x => x.Items)
            .WithOne(i => i.Checklist)
            .HasForeignKey(i => i.ChecklistId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
