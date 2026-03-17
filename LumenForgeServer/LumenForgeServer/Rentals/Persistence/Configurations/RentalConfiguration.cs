using LumenForgeServer.Common;
using LumenForgeServer.Rentals.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Rentals.Persistence.Configurations;

/// <summary>
/// Entity configuration for <see cref="Rental"/>.
/// </summary>
public sealed class RentalConfiguration : IEntityTypeConfiguration<Rental>
{
    public void Configure(EntityTypeBuilder<Rental> builder)
    {
        builder.ToTable("rental");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Uuid).IsUnique();

        builder.Property(x => x.CustomerUserId).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => x.CustomerUserId);

        builder.Property(x => x.RentalStatus)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.OwnsOne(x => x.Request, r =>
        {
            r.Property(x => x.Title).HasColumnName("RequestTitle").HasMaxLength(512);
            r.Property(x => x.Description).HasColumnName("RequestDescription").HasMaxLength(4000);
            r.Property(x => x.EventName).HasColumnName("EventName").HasMaxLength(512);
            r.Property(x => x.CustomerNotes).HasColumnName("CustomerNotes").HasMaxLength(4000);
            r.Property(x => x.DeliveryAddress).HasColumnName("DeliveryAddress").HasMaxLength(1000);
            r.Property(x => x.Priority).HasColumnName("Priority").HasConversion<string>().HasMaxLength(16).IsRequired();
        });

        builder.OwnsOne(x => x.Schedule, s =>
        {
            s.Property(x => x.RequestedAt).HasColumnName("RequestedAt");
            s.Property(x => x.PlannedPickupAt).HasColumnName("PlannedPickupAt");
            s.Property(x => x.PlannedReturnAt).HasColumnName("PlannedReturnAt");
            s.Property(x => x.PickupAt).HasColumnName("PickupAt");
            s.Property(x => x.DropoffAt).HasColumnName("DropoffAt");
        });

        builder.OwnsOne(x => x.Assignment, a =>
        {
            a.Property(x => x.AssignedByUserId).HasColumnName("AssignedByUserId").HasMaxLength(128);
            a.Property(x => x.AssignedAt).HasColumnName("AssignedAt");
            a.Property(x => x.PickupProcessedByUserId).HasColumnName("PickupProcessedByUserId").HasMaxLength(128);
            a.Property(x => x.DropoffProcessedByUserId).HasColumnName("DropoffProcessedByUserId").HasMaxLength(128);
            a.Property(x => x.CompletedByUserId).HasColumnName("CompletedByUserId").HasMaxLength(128);
        });

        builder.OwnsOne(x => x.Scrap, s =>
        {
            s.Property(x => x.IsScrapped).HasColumnName("IsScrapped");
            s.Property(x => x.ScrappedAt).HasColumnName("ScrappedAt");
            s.Property(x => x.ScrappedByUserId).HasColumnName("ScrappedByUserId").HasMaxLength(128);
        });

        builder.HasOne(x => x.RentalReport)
            .WithOne(rr => rr.Rental)
            .HasForeignKey<RentalReport>(rr => rr.RentalId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
