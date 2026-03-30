using LumenForgeServer.Rentals.Domain;

namespace LumenForgeServer.Rentals.Persistence;

public interface IQuestionRepository
{
    /// <summary>
    /// Executes the get random questions async operation.
    /// Core concept: maps application requests to persistence queries and materializes domain data.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="amount">Numeric input used by this operation.</param>
    /// <returns>A task that resolves to the List&lt;Question&gt; result.</returns>
    public Task<List<Question>> GetRandomQuestionsAsync(int amount);
    
    /// <summary>
    /// Executes the does question exist by guid async operation.
    /// Core concept: maps application requests to persistence queries and materializes domain data.
    /// </summary>
    /// <remarks>Potential side effects: may execute database writes or update tracked entity state in the current DbContext.</remarks>
    /// <param name="questionGuids">Unique identifier used to target the requested entity.</param>
    /// <returns>A task that resolves to the int result.</returns>
    public Task<int> DoesQuestionExistByGuidAsync(List<Guid> questionGuids);

    /// <summary>
    /// Executes the get question ids by guid async operation.
    /// Core concept: maps application requests to persistence queries and materializes domain data.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="questionGuids">Unique identifier used to target the requested entity.</param>
    /// <returns>A task that resolves to the Dictionary&lt;Guid, long&gt; result.</returns>
    public Task<Dictionary<Guid, long>> GetQuestionIdsByGuidAsync(List<Guid> questionGuids);
}
