using LumenForgeServer.Billing.Domain;
using LumenForgeServer.Rentals.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Billing.Persistence.Configurations;

/// <summary>
/// Entity configuration for <see cref="Invoice"/>.
/// </summary>
public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    /// <summary>
    /// Executes the configure operation.
    /// Core concept: maps application requests to persistence queries and materializes domain data.
    /// </summary>
    /// <remarks>Potential side effects: may execute database writes or update tracked entity state in the current DbContext.</remarks>
    /// <param name="builder">Input value used by this operation.</param>
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoice");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Uuid).IsUnique();

        builder.Property(x => x.InvoiceNumber).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => x.InvoiceNumber).IsUnique();

        builder.Property(x => x.SubtotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.TaxAmount).HasPrecision(18, 2);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);

        builder.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(x => x.InvoiceDocumentUrl).HasMaxLength(2000);

        builder.Property(x => x.GeneratedByUserId).HasMaxLength(128);

        builder.HasOne(x => x.Rental)
            .WithMany(r => r.Invoices)
            .HasForeignKey(x => x.RentalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.InvoiceStatus)
            .WithMany(s => s.Invoices)
            .HasForeignKey(x => x.InvoiceStatusId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
