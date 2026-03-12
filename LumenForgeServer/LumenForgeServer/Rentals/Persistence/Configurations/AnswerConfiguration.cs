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
        builder.HasIndex(x => x.Uuid).IsUnique();

        builder.Property(x => x.Response).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Comment).HasMaxLength(2000);
        builder.Property(x => x.RespondentUserId).HasMaxLength(128);

        builder.HasOne(x => x.Question)
            .WithMany(q => q.Answers)
            .HasForeignKey(x => x.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.RentalEvent)
            .WithMany(re => re.Answers)
            .HasForeignKey(x => x.RentalEventId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Rental)
            .WithMany()
            .HasForeignKey(x => x.RentalId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.QuestionId, x.RentalId });
        builder.HasIndex(x => x.CreatedAt);
    }
}
