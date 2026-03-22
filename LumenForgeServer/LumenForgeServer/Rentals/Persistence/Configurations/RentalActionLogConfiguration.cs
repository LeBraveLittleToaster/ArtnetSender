using LumenForgeServer.Rentals.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Rentals.Persistence.Configurations;

/// <summary>
/// Entity configuration for <see cref="RentalActionLog"/>.
/// </summary>
public sealed class RentalActionLogConfiguration : IEntityTypeConfiguration<RentalActionLog>
{
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
