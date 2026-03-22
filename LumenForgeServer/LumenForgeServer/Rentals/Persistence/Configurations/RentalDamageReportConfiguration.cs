using LumenForgeServer.Rentals.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Rentals.Persistence.Configurations;

/// <summary>
/// Entity configuration for <see cref="RentalDamageReport"/>.
/// </summary>
public sealed class RentalDamageReportConfiguration : IEntityTypeConfiguration<RentalDamageReport>
{
    public void Configure(EntityTypeBuilder<RentalDamageReport> builder)
    {
        builder.ToTable("rental_damage_report");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Guid).IsUnique();

        builder.Property(x => x.Description).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.Severity).IsRequired();
        builder.Property(x => x.ReportedByKcId).HasMaxLength(128).IsRequired();

        builder.HasIndex(x => x.ProcessInstanceId);
        builder.HasIndex(x => x.ReportedAt);
    }
}
