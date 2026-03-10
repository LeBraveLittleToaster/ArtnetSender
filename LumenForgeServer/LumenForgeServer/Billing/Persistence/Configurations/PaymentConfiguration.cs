using LumenForgeServer.Billing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Billing.Persistence.Configurations;

/// <summary>
/// Entity configuration for <see cref="Payment"/>.
/// </summary>
public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payment");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Uuid).IsUnique();

        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.ProviderReference).HasMaxLength(256);

        builder.HasOne(x => x.Invoice)
            .WithMany(i => i.Payments)
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.PaymentStatus)
            .WithMany(s => s.Payments)
            .HasForeignKey(x => x.PaymentStatusId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
