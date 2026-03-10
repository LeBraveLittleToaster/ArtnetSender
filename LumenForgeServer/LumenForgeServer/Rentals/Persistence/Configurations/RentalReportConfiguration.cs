using LumenForgeServer.Rentals.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Rentals.Persistence.Configurations;

/// <summary>
/// Entity configuration for <see cref="RentalReport"/>.
/// </summary>
public sealed class RentalReportConfiguration : IEntityTypeConfiguration<RentalReport>
{
    public void Configure(EntityTypeBuilder<RentalReport> builder)
    {
        builder.ToTable("rental_report");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.HasIndex(x => x.RentalId).IsUnique();

        builder.Property(x => x.GeneratedByUserId).HasMaxLength(128);
        builder.Property(x => x.ReportSummary).HasMaxLength(4000);
        builder.Property(x => x.ReportDocumentUrl).HasMaxLength(2000);
    }
}
