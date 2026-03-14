using LumenForgeServer.Catalogue.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Catalogue.Persistence.Configurations;

/// <summary>
/// Entity configuration for <see cref="CatalogueItem"/>.
/// </summary>
public sealed class CatalogueItemConfiguration : IEntityTypeConfiguration<CatalogueItem>
{
    public void Configure(EntityTypeBuilder<CatalogueItem> builder)
    {
        builder.ToTable("catalogue_item");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Guid).IsUnique();
        builder.HasIndex(x => x.DeviceId).IsUnique();

        builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.PhotoUrl).HasMaxLength(2000);
        builder.Property(x => x.IsPublished).IsRequired();
        builder.Property(x => x.SortOrder).IsRequired();

        builder.HasOne(x => x.Device)
            .WithMany()
            .HasForeignKey(x => x.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}