using LumenForgeServer.Rentals.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Rentals.Persistence.Configurations;

/// <summary>
/// Entity configuration for <see cref="Answer"/>.
/// </summary>
public sealed class AnswerConfiguration : IEntityTypeConfiguration<Answer>
{
    public void Configure(EntityTypeBuilder<Answer> builder)
    {
        builder.ToTable("answer");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Guid).IsUnique();

        builder.Property(x => x.Value).HasMaxLength(4000).IsRequired();

        builder.HasIndex(x => x.RentalId);
        builder.HasIndex(x => x.QuestionId);
        
        builder.HasOne(a => a.Rental)
            .WithMany(r => r.Answers)
            .HasForeignKey(a => a.RentalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Question)
            .WithMany(q => q.Answers)
            .HasForeignKey(a => a.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.RentalId, a.QuestionId }).IsUnique();
    }
}
