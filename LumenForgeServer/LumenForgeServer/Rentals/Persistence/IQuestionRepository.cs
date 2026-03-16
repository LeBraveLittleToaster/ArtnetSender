using LumenForgeServer.Rentals.Domain;

namespace LumenForgeServer.Rentals.Persistence;

/// <summary>
/// Persistence contract for rental survey questions and answers.
/// </summary>
public interface IQuestionRepository
{
    Task<Question?> GetQuestionByGuidAsync(Guid questionGuid, CancellationToken ct);
    
    /// <summary>
    /// Returns all active questions ordered by display order.
    /// </summary>
    Task<IReadOnlyList<Question>> ListActiveQuestionsAsync(CancellationToken ct);

    /// <summary>
    /// Returns all questions (including inactive ones), useful for admin views.
    /// </summary>
    Task<(IReadOnlyList<Question> items, long total)> ListAllQuestionsAsync(
        string? search,
        int limit,
        int offset,
        CancellationToken ct);

    /// <summary>
    /// Returns <paramref name="count"/> active questions in a random order.
    /// </summary>
    Task<IReadOnlyList<Question>> GetRandomActiveQuestionsAsync(int count, CancellationToken ct);

    Task AddQuestionAsync(Question question, CancellationToken ct);
    Task DeleteQuestionAsync(Question question, CancellationToken ct);

    Task<Answer?> GetAnswerByGuidAsync(Guid answerGuid, CancellationToken ct);

    /// <summary>
    /// Returns answers for a specific question, optionally filtered by rental.
    /// </summary>
    Task<IReadOnlyList<Answer>> ListAnswersForQuestionAsync(
        Guid questionGuid,
        Guid? rentalGuid,
        CancellationToken ct);

    Task AddAnswerAsync(Answer answer, CancellationToken ct);
    Task DeleteAnswerAsync(Answer answer, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
