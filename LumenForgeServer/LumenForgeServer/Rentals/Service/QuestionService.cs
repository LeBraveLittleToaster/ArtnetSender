using LumenForgeServer.Common.Exceptions;
using LumenForgeServer.Rentals.Domain;
using LumenForgeServer.Rentals.Dto.Command;
using LumenForgeServer.Rentals.Dto.Query;
using LumenForgeServer.Rentals.Dto.View;
using LumenForgeServer.Rentals.Persistence;
using NodaTime;

namespace LumenForgeServer.Rentals.Service;

/// <summary>
/// Application service for rental survey questions and answers.
/// Provides read-only public access to questions and accept answer submissions.
/// </summary>
public class QuestionService(IQuestionRepository repository, IRentalRepository rentalRepository)
{
    private const int RandomQuestionCount = 10;

    /// <summary>
    /// Returns all active survey questions ordered by display order.
    /// Public endpoint — no authentication required.
    /// </summary>
    public async Task<(IReadOnlyList<QuestionView> items, long total)> ListActiveQuestionsAsync(int limit, int offset, CancellationToken ct)
    {
        var (questions, total) = await repository.ListActiveQuestionsAsync(limit, offset, ct);
        return (questions.Select(q => QuestionView.FromEntity(q)).ToList(), total);
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
    public async Task<(IReadOnlyList<AnswerView> items, long total)> ListAnswersForQuestionAsync(
        Guid questionUuid,
        Guid? rentalGuid,
        int limit,
        int offset,
        CancellationToken ct)
    {
        _ = await repository.GetQuestionByGuidAsync(questionUuid, ct)
            ?? throw new NotFoundException($"Question '{questionUuid}' not found.");

        var (answers, total) = await repository.ListAnswersForQuestionAsync(questionUuid, rentalGuid, limit, offset, ct);
        return (answers.Select(a => AnswerView.FromEntity(a)).ToList(), total);
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

    /// <summary>
    /// Returns a list of survey questions relevant to the provided event context.
    /// Currently selects <see cref="RandomQuestionCount"/> active questions at random;
    /// a proper recommender model will replace this in a future iteration.
    /// </summary>
    public async Task<IReadOnlyList<QuestionView>> GetQuestionsForEventAsync(
        EventContextDto dto,
        CancellationToken ct)
    {
        var questions = await repository.GetRandomActiveQuestionsAsync(RandomQuestionCount, ct);
        return questions.Select(q => QuestionView.FromEntity(q)).ToList();
    }

    /// <summary>
    /// Submits answers to all survey questions for one rental in a single transaction.
    /// </summary>
    public async Task<IReadOnlyList<AnswerView>> SubmitAnswersBulkAsync(
        SubmitAnswersBulkDto dto,
        string? respondentUserId,
        CancellationToken ct)
    {
        var rental = await rentalRepository.GetRentalByGuidAsync(dto.RentalUuid, RentalInclude.None, ct)
            ?? throw new NotFoundException($"Rental '{dto.RentalUuid}' not found.");

        var requestedGuids = dto.Answers.Select(a => a.QuestionUuid).Distinct().ToList();
        var questions = await repository.GetQuestionsByGuidsAsync(requestedGuids, ct);
        var questionMap = questions.ToDictionary(q => q.Uuid);

        var missingGuids = requestedGuids.Except(questionMap.Keys).ToList();
        if (missingGuids.Count > 0)
            throw new NotFoundException($"Questions not found: {string.Join(", ", missingGuids)}.");

        var invalidEntries = dto.Answers
            .Where(a => !IsValidResponse(a.Response))
            .Select(a => a.QuestionUuid.ToString())
            .ToList();

        if (invalidEntries.Count > 0)
            throw new ValidationException(
                "One or more answers contain an invalid response value.",
                new Dictionary<string, string[]>
                {
                    ["response"] = [$"Must be one of: Yes, No, NotImportant, Unknown. Invalid entries: {string.Join(", ", invalidEntries)}"]
                });

        var now = SystemClock.Instance.GetCurrentInstant();
        var answers = dto.Answers
            .Select(entry => new Answer
            {
                Uuid = Guid.CreateVersion7(),
                QuestionId = questionMap[entry.QuestionUuid].Id,
                RentalId = rental.Id,
                Response = entry.Response,
                Comment = entry.Comment?.Trim(),
                RespondentUserId = respondentUserId,
                CreatedAt = now,
                UpdatedAt = now,
            })
            .ToList();

        await repository.AddAnswersAsync(answers, ct);
        await repository.SaveChangesAsync(ct);

        return dto.Answers
            .Select((entry, i) => new AnswerView
            {
                Uuid = answers[i].Uuid,
                QuestionUuid = entry.QuestionUuid,
                QuestionText = questionMap[entry.QuestionUuid].QuestionText,
                Response = entry.Response,
                Comment = entry.Comment?.Trim(),
                RespondentUserId = respondentUserId,
                RentalUuid = dto.RentalUuid,
                CreatedAt = now,
            })
            .ToList();
    }

    private static bool IsValidResponse(string response) =>
        response is "Yes" or "No" or "NotImportant" or "Unknown";
}
