using LumenForgeServer.Rentals.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Rentals.Persistence.Configurations;

/// <summary>
/// Entity configuration for <see cref="RentalProcessInstance"/>.
/// </summary>
public sealed class RentalProcessInstanceConfiguration : IEntityTypeConfiguration<RentalProcessInstance>
{
    public void Configure(EntityTypeBuilder<RentalProcessInstance> builder)
    {
        builder.ToTable("rental_process_instance");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Guid).IsUnique();

        builder.Property(x => x.CurrentStage).IsRequired();
        builder.Property(x => x.CreatedByKcId).HasMaxLength(128).IsRequired();

        builder.HasIndex(x => x.CurrentStage);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.UpdatedAt);

        builder.HasOne(x => x.Rental)
            .WithOne(r => r.ProcessInstance)
            .HasForeignKey<RentalProcessInstance>(x => x.RentalId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.ActionLogs)
            .WithOne(l => l.ProcessInstance)
            .HasForeignKey(l => l.ProcessInstanceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Checklists)
            .WithOne(c => c.ProcessInstance)
            .HasForeignKey(c => c.ProcessInstanceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Extensions)
            .WithOne(e => e.ProcessInstance)
            .HasForeignKey(e => e.ProcessInstanceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.DamageReports)
            .WithOne(d => d.ProcessInstance)
            .HasForeignKey(d => d.ProcessInstanceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
