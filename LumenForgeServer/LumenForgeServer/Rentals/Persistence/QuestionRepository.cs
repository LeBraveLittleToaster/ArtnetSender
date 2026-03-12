using LumenForgeServer.Common.Database;
using LumenForgeServer.Rentals.Domain;
using Microsoft.EntityFrameworkCore;

namespace LumenForgeServer.Rentals.Persistence;

/// <summary>
/// EF Core-backed repository for rental survey questions and answers.
/// </summary>
public sealed class QuestionRepository(AppDbContext db) : IQuestionRepository
{
    public Task<Question?> GetQuestionByGuidAsync(Guid questionGuid, CancellationToken ct)
        => db.Questions
            .Include(q => q.Answers)
            .SingleOrDefaultAsync(q => q.Uuid == questionGuid, ct);

    public async Task<IReadOnlyList<Question>> ListActiveQuestionsAsync(CancellationToken ct)
        => await db.Questions
            .AsNoTracking()
            .Where(q => q.IsActive)
            .OrderBy(q => q.DisplayOrder)
            .ToListAsync(ct);

    public async Task<(IReadOnlyList<Question> items, long total)> ListAllQuestionsAsync(
        string? search,
        int limit,
        int offset,
        CancellationToken ct)
    {
        var query = db.Questions.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(q =>
                q.QuestionText.Contains(search) ||
                (q.Category != null && q.Category.Contains(search)));
        }

        var total = await query.LongCountAsync(ct);
        var items = await query
            .AsNoTracking()
            .OrderBy(q => q.DisplayOrder)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task AddQuestionAsync(Question question, CancellationToken ct)
        => db.Questions.AddAsync(question, ct).AsTask();

    public Task DeleteQuestionAsync(Question question, CancellationToken ct)
    {
        db.Questions.Remove(question);
        return Task.CompletedTask;
    }

    public Task<Answer?> GetAnswerByGuidAsync(Guid answerGuid, CancellationToken ct)
        => db.Answers
            .Include(a => a.Question)
            .Include(a => a.Rental)
            .SingleOrDefaultAsync(a => a.Uuid == answerGuid, ct);

    public async Task<IReadOnlyList<Answer>> ListAnswersForQuestionAsync(
        Guid questionGuid,
        Guid? rentalGuid,
        CancellationToken ct)
    {
        var query = db.Answers
            .Include(a => a.Question)
            .Where(a => a.Question.Uuid == questionGuid)
            .AsQueryable();

        if (rentalGuid.HasValue)
        {
            query = query.Where(a => a.Rental != null && a.Rental.Uuid == rentalGuid.Value);
        }

        return await query
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);
    }

    public Task AddAnswerAsync(Answer answer, CancellationToken ct)
        => db.Answers.AddAsync(answer, ct).AsTask();

    public Task DeleteAnswerAsync(Answer answer, CancellationToken ct)
    {
        db.Answers.Remove(answer);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct)
        => db.SaveChangesAsync(ct);
}
