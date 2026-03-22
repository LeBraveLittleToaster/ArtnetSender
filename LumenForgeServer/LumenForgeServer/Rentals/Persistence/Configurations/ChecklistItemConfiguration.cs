using LumenForgeServer.Rentals.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Rentals.Persistence.Configurations;

/// <summary>
/// Entity configuration for <see cref="ChecklistItem"/>.
/// </summary>
public sealed class ChecklistItemConfiguration : IEntityTypeConfiguration<ChecklistItem>
{
    public void Configure(EntityTypeBuilder<ChecklistItem> builder)
    {
        builder.ToTable("checklist_item");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Guid).IsUnique();

        builder.Property(x => x.DeviceName).HasMaxLength(512).IsRequired();
        builder.Property(x => x.ScannedValue).HasMaxLength(512);
        builder.Property(x => x.ScannedByKcId).HasMaxLength(128);

        builder.HasIndex(x => x.ChecklistId);
    }
}
