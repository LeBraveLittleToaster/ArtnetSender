using LumenForgeServer.Billing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Billing.Persistence.Configurations;

/// <summary>
/// Entity configuration for <see cref="PaymentStatus"/>.
/// </summary>
public sealed class PaymentStatusConfiguration : IEntityTypeConfiguration<PaymentStatus>
{
    public void Configure(EntityTypeBuilder<PaymentStatus> builder)
    {
        builder.ToTable("payment_status");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Uuid).IsUnique();
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.HasIndex(x => x.Name).IsUnique();
    }
}
