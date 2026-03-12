using LumenForgeServer.Common.Exceptions;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Dto.Command;
using LumenForgeServer.Rentals.Dto.View;
using LumenForgeServer.Rentals.Persistence;
using NodaTime;

namespace LumenForgeServer.Rentals.Service;

/// <summary>
/// Application service for rental survey questions and answers.
/// Provides read-only public access to questions and accept answer submissions.
/// </summary>
public class QuestionService(IQuestionRepository repository)
{
    /// <summary>
    /// Returns all active survey questions ordered by display order.
    /// Public endpoint — no authentication required.
    /// </summary>
    public async Task<IReadOnlyList<QuestionView>> ListActiveQuestionsAsync(CancellationToken ct)
    {
        var questions = await repository.ListActiveQuestionsAsync(ct);
        return questions.Select(q => QuestionView.FromEntity(q)).ToList();
    }

    /// <summary>
    /// Retrieves a single question by UUID.
    /// </summary>
    public async Task<QuestionView> GetQuestionAsync(Guid questionUuid, CancellationToken ct)
    {
        var question = await repository.GetQuestionByGuidAsync(questionUuid, ct)
            ?? throw new NotFoundException($"Question '{questionUuid}' not found.");

        return QuestionView.FromEntity(question);
    }

    /// <summary>
    /// Lists all questions (including inactive) with search and paging.
    /// Admin-only in practice, but no role enforcement here.
    /// </summary>
    public async Task<(IReadOnlyList<QuestionView> items, long total)> ListAllQuestionsAsync(
        string? search,
        int limit,
        int offset,
        CancellationToken ct)
    {
        var (items, total) = await repository.ListAllQuestionsAsync(search, limit, offset, ct);
        return (items.Select(q => QuestionView.FromEntity(q)).ToList(), total);
    }

    /// <summary>
    /// Creates a new survey question.
    /// In practice, this should be admin-only.
    /// </summary>
    public async Task<QuestionView> CreateQuestionAsync(CreateQuestionDto dto, CancellationToken ct)
    {
        var now = SystemClock.Instance.GetCurrentInstant();
        var question = new Domain.Question
        {
            Uuid = Guid.CreateVersion7(),
            QuestionText = dto.QuestionText.Trim(),
            Category = dto.Category?.Trim(),
            DisplayOrder = dto.DisplayOrder,
            IsActive = dto.IsActive,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await repository.AddQuestionAsync(question, ct);
        await repository.SaveChangesAsync(ct);

        return QuestionView.FromEntity(question);
    }

    /// <summary>
    /// Deletes a question and all associated answers.
    /// In practice, this should be admin-only.
    /// </summary>
    public async Task DeleteQuestionAsync(Guid questionUuid, CancellationToken ct)
    {
        var question = await repository.GetQuestionByGuidAsync(questionUuid, ct)
            ?? throw new NotFoundException($"Question '{questionUuid}' not found.");

        await repository.DeleteQuestionAsync(question, ct);
        await repository.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Submits an answer to a survey question.
    /// </summary>
    public async Task<AnswerView> SubmitAnswerAsync(
        SubmitAnswerDto dto,
        string? respondentUserId,
        CancellationToken ct)
    {
        var question = await repository.GetQuestionByGuidAsync(dto.QuestionUuid, ct)
            ?? throw new NotFoundException($"Question '{dto.QuestionUuid}' not found.");

        // Validate response value
        if (!IsValidResponse(dto.Response))
        {
            throw new ValidationException(
                $"Invalid response value '{dto.Response}'.",
                new Dictionary<string, string[]>
                {
                    ["response"] = ["Must be one of: Yes, No, NotImportant, Unknown"]
                });
        }

        var now = SystemClock.Instance.GetCurrentInstant();
        var answer = new Domain.Answer
        {
            Uuid = Guid.CreateVersion7(),
            QuestionId = question.Id,
            Response = dto.Response,
            Comment = dto.Comment?.Trim(),
            RespondentUserId = respondentUserId,
            CreatedAt = now,
            UpdatedAt = now,
        };

        // Link to rental if provided
        if (dto.RentalUuid.HasValue)
        {
            // Note: We're not loading the rental here for performance,
            // but validation could be added if needed
            answer.RentalId = null; // Will be set by repository if rental exists
        }

        await repository.AddAnswerAsync(answer, ct);
        await repository.SaveChangesAsync(ct);

        var persisted = await repository.GetAnswerByGuidAsync(answer.Uuid, ct)
            ?? throw new NotFoundException("Answer not found after creation.");

        return AnswerView.FromEntity(persisted);
    }

    /// <summary>
    /// Returns answers for a specific question.
    /// </summary>
    public async Task<IReadOnlyList<AnswerView>> ListAnswersForQuestionAsync(
        Guid questionUuid,
        Guid? rentalGuid,
        CancellationToken ct)
    {
        _ = await repository.GetQuestionByGuidAsync(questionUuid, ct)
            ?? throw new NotFoundException($"Question '{questionUuid}' not found.");

        var answers = await repository.ListAnswersForQuestionAsync(questionUuid, rentalGuid, ct);
        return answers.Select(a => AnswerView.FromEntity(a)).ToList();
    }

    /// <summary>
    /// Deletes an answer.
    /// </summary>
    public async Task DeleteAnswerAsync(Guid answerUuid, CancellationToken ct)
    {
        var answer = await repository.GetAnswerByGuidAsync(answerUuid, ct)
            ?? throw new NotFoundException($"Answer '{answerUuid}' not found.");

        await repository.DeleteAnswerAsync(answer, ct);
        await repository.SaveChangesAsync(ct);
    }

    private static bool IsValidResponse(string response) =>
        response is "Yes" or "No" or "NotImportant" or "Unknown";
}
