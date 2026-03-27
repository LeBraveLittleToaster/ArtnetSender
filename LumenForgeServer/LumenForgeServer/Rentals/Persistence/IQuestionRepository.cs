using LumenForgeServer.Rentals.Domain;

namespace LumenForgeServer.Rentals.Persistence;

public interface IQuestionRepository
{
    public Task<List<Question>> GetRandomQuestionsAsync(int amount);
    
    public Task<int> DoesQuestionExistByGuidAsync(List<Guid> questionGuids);

    public Task<Dictionary<Guid, long>> GetQuestionIdsByGuidAsync(List<Guid> questionGuids);
}
