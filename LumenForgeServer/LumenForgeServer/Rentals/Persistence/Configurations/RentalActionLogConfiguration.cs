using LumenForgeServer.Rentals.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Rentals.Persistence.Configurations;

/// <summary>
/// Entity configuration for <see cref="RentalActionLog"/>.
/// </summary>
public sealed class RentalActionLogConfiguration : IEntityTypeConfiguration<RentalActionLog>
{
    /// <summary>
    /// Executes the configure operation.
    /// Core concept: maps application requests to persistence queries and materializes domain data.
    /// </summary>
    /// <remarks>Potential side effects: may execute database writes or update tracked entity state in the current DbContext.</remarks>
    /// <param name="builder">Input value used by this operation.</param>
    public void Configure(EntityTypeBuilder<RentalActionLog> builder)
    {
        builder.ToTable("rental_action_log");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Guid).IsUnique();

        builder.Property(x => x.ActionType).IsRequired();
        builder.Property(x => x.PerformedByKcId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.StageBefore).IsRequired();
        builder.Property(x => x.StageAfter).IsRequired();
        builder.Property(x => x.ErrorMessage).HasMaxLength(4000);
        builder.Property(x => x.PayloadJson).HasColumnType("jsonb");

        builder.HasIndex(x => x.PerformedAt);
        builder.HasIndex(x => x.ProcessInstanceId);
    }
}
