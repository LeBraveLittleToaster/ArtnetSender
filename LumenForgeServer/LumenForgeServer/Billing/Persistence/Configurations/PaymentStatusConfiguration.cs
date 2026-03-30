using LumenForgeServer.Billing.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Billing.Persistence.Configurations;

/// <summary>
/// Entity configuration for <see cref="PaymentStatus"/>.
/// </summary>
public sealed class PaymentStatusConfiguration : IEntityTypeConfiguration<PaymentStatus>
{
    /// <summary>
    /// Executes the configure operation.
    /// Core concept: maps application requests to persistence queries and materializes domain data.
    /// </summary>
    /// <remarks>Potential side effects: may execute database writes or update tracked entity state in the current DbContext.</remarks>
    /// <param name="builder">Input value used by this operation.</param>
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
