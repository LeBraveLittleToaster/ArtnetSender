using LumenForgeServer.Rentals.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LumenForgeServer.Rentals.Persistence.Configurations;

/// <summary>
/// Entity configuration for <see cref="Answer"/>.
/// </summary>
public sealed class AnswerConfiguration : IEntityTypeConfiguration<Answer>
{
    /// <summary>
    /// Executes the configure operation.
    /// Core concept: maps application requests to persistence queries and materializes domain data.
    /// </summary>
    /// <remarks>Potential side effects: may execute database writes or update tracked entity state in the current DbContext.</remarks>
    /// <param name="builder">Input value used by this operation.</param>
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
