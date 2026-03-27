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