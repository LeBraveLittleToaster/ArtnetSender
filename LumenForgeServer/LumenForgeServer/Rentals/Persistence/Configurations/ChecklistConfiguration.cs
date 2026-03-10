using LumenForgeServer.Rentals.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Rentals.Persistence.Configurations;

/// <summary>
/// Entity configuration for <see cref="Checklist"/>.
/// </summary>
public sealed class ChecklistConfiguration : IEntityTypeConfiguration<Checklist>
{
    public void Configure(EntityTypeBuilder<Checklist> builder)
    {
        builder.ToTable("checklist");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Uuid).IsUnique();

        builder.Property(x => x.ChecklistType).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(4000);

        builder.Property(x => x.GeneratedByUserId).HasMaxLength(128);
        builder.Property(x => x.SignedByUserId).HasMaxLength(128);

        builder.HasOne(x => x.Rental)
            .WithMany(r => r.Checklists)
            .HasForeignKey(x => x.RentalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.SourceChecklist)
            .WithMany(x => x.DerivedChecklists)
            .HasForeignKey(x => x.SourceChecklistId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.RentalId, x.ChecklistType }).IsUnique(false);
    }
}
