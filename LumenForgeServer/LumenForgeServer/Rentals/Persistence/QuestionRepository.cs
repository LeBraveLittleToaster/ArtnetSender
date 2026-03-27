using LumenForgeServer.Common.Database;
using LumenForgeServer.Rentals.Domain;

namespace LumenForgeServer.Rentals.Persistence;

public class QuestionRepository(AppDbContext context) : IQuestionRepository
{
    public Task<List<Question>> GetRandomQuestionsAsync(int amount)
    {
        return Task.FromResult(context.Questions
            .Where(q => q.IsActive)
            .OrderBy(o => Guid.NewGuid())
            .Take(amount)
            .ToList());
    }

    public Task<int> DoesQuestionExistByGuidAsync(List<Guid> questionGuids)
    {
        return Task.FromResult(questionGuids.Count - context.Questions
            .Count(q => questionGuids.Contains(q.Guid)));
    }

    public Task<Dictionary<Guid, long>> GetQuestionIdsByGuidAsync(List<Guid> questionGuids)
    {
        return Task.FromResult(context.Questions
            .Where(q => questionGuids.Contains(q.Guid))
            .ToDictionary(q => q.Guid, q => q.Id));
    }
}
