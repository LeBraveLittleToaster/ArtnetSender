using LumenForgeServer.Rentals.Dto.View;
using LumenForgeServer.Rentals.Persistence;

namespace LumenForgeServer.Rentals.Service;

public sealed class QuestionService(IQuestionRepository questionRepository)
{
    public async Task<List<QuestionView>> GetRandomQuestionsAsync(int amount)
    {
        var questions = await questionRepository.GetRandomQuestionsAsync(amount);
        return questions.Select(QuestionView.FromEntity).ToList();
    } 
}