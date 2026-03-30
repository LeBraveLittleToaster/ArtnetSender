using LumenForgeServer.Rentals.Dto.Command;
using LumenForgeServer.Rentals.Dto.Query;
using LumenForgeServer.Rentals.Service;
using Microsoft.AspNetCore.Mvc;

namespace LumenForgeServer.Rentals.Controller;

[Route("api/v1/rentals/questions")]
[ApiController]
[Tags("Rentals – Questions")]
public class RentalQuestionController(QuestionService qService) : ControllerBase
{
    private const int NumberOfRentalQuestions = 3;
    
    /// <summary>
    /// Executes the get rental questions operation.
    /// Core concept: handles the HTTP endpoint contract and delegates business logic to services.
    /// </summary>
    /// <remarks>Potential side effects: read-only operation with no intended state mutation.</remarks>
    /// <param name="input">Request payload containing the input data required for the operation.</param>
    /// <returns>A task that resolves to the IActionResult result.</returns>
    [HttpPost("")]
    [Produces("application/json")]
    public async Task<IActionResult> GetRentalQuestions([FromBody] CreateRentalFormInput input)
    {
        var questions = await qService.GetRandomQuestionsAsync(NumberOfRentalQuestions);
        return Ok(new RentalQuestionsDto()
        {
            Questions = questions
        });
    }
}