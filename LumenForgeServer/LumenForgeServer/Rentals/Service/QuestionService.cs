using LumenForgeServer.Rentals.Dto.View;
using LumenForgeServer.Rentals.Persistence;

namespace LumenForgeServer.Rentals.Service;

public sealed class QuestionService(IQuestionRepository questionRepository)
{
    /// <summary>
    /// Executes the get random questions async operation.
    /// Core concept: applies domain rules and coordinates repository/service calls for this use case.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="amount">Numeric input used by this operation.</param>
    /// <returns>A task that resolves to the List&lt;QuestionView&gt; result.</returns>
    public async Task<List<QuestionView>> GetRandomQuestionsAsync(int amount)
    {
        var questions = await questionRepository.GetRandomQuestionsAsync(amount);
        return questions.Select(QuestionView.FromEntity).ToList();
    } 
}
