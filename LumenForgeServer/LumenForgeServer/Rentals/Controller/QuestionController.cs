using LumenForgeServer.Auth.Domain;
using LumenForgeServer.Rentals.Dto.Command;
using LumenForgeServer.Rentals.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LumenForgeServer.Rentals.Controller;

/// <summary>
/// HTTP API for rental survey questions and answer submissions.
/// </summary>
/// <remarks>
/// Routes are under <c>api/v1/rentals/surveys</c>.
/// The questions endpoint is public (authenticated but no roles required).
/// </remarks>
[Route("api/v1/rentals/surveys")]
[ApiController]
[Authorize]
public class QuestionController(QuestionService questionService) : ControllerBase
{
    // =========================================================================
    // Questions — Public read-only endpoints
    // =========================================================================

    /// <summary>
    /// Returns all active survey questions.
    /// Public endpoint — authenticated users can view questions.
    /// </summary>
    /// <remarks>
    /// No role required — any authenticated user can view active questions.
    /// </remarks>
    [HttpGet("questions")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [Produces("application/json")]
    public async Task<IActionResult> ListActiveQuestions(
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0,
        CancellationToken ct = default)
    {
        var (items, total) = await questionService.ListActiveQuestionsAsync(limit, offset, ct);
        return Ok(new { list = items, total });
    }

    [HttpGet("questions/{questionGuid:Guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> GetQuestion(
        [FromRoute] Guid questionGuid,
        CancellationToken ct)
    {
        var question = await questionService.GetQuestionAsync(questionGuid, ct);
        return Ok(question);
    }

    // =========================================================================
    // Questions — Admin CRUD (create/delete functions)
    // Note: In production, restrict these with [Authorize(Roles = ...)]
    // =========================================================================

    [HttpGet("questions/all")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public async Task<IActionResult> ListAllQuestions(
        [FromQuery] string? search,
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0,
        CancellationToken ct = default)
    {
        var (items, total) = await questionService.ListAllQuestionsAsync(search, limit, offset, ct);
        return Ok(new { list = items, total });
    }

    [HttpPut("questions")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("application/json")]
    public async Task<IActionResult> CreateQuestion(
        [FromBody] CreateQuestionDto dto,
        CancellationToken ct)
    {
        var question = await questionService.CreateQuestionAsync(dto, ct);
        return CreatedAtAction(nameof(GetQuestion), new { questionGuid = question.Uuid }, question);
    }

    [HttpDelete("questions/{questionGuid:Guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> DeleteQuestion(
        [FromRoute] Guid questionGuid,
        CancellationToken ct)
    {
        await questionService.DeleteQuestionAsync(questionGuid, ct);
        return NoContent();
    }

    [HttpPost("questions/recommend")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [Produces("application/json")]
    public async Task<IActionResult> RecommendQuestions(
        [FromBody] EventContextDto dto,
        CancellationToken ct)
    {
        var questions = await questionService.GetQuestionsForEventAsync(dto, ct);
        return Ok(questions);
    }

    // =========================================================================
    // Answers — Submission and retrieval
    // =========================================================================

    /// <summary>
    /// Submits an answer to a survey question.
    /// </summary>
    /// <remarks>
    /// Authenticated users can submit answers to any active question.
    /// The user's Keycloak ID is automatically captured.
    /// </remarks>
    [HttpPost("questions/{questionGuid:Guid}/answers")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> SubmitAnswer(
        [FromRoute] Guid questionGuid,
        [FromBody] SubmitAnswerDto dto,
        CancellationToken ct)
    {
        if (questionGuid != dto.QuestionUuid)
            return BadRequest(new { error = "Question UUID mismatch between route and payload." });

        var respondentUserId = User.FindFirstValue("sub")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.Identity?.Name;

        var answer = await questionService.SubmitAnswerAsync(dto, respondentUserId, ct);
        return CreatedAtAction(nameof(GetAnswer), new { answerGuid = answer.Uuid }, answer);
    }

    /// <summary>
    /// Submits answers to all survey questions for one rental in a single request.
    /// </summary>
    /// <remarks>
    /// All answers are validated and persisted in a single transaction.
    /// The rental must exist; all question UUIDs must resolve to active questions.
    /// </remarks>
    [HttpPost("answers")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> SubmitAnswersBulk(
        [FromBody] SubmitAnswersBulkDto dto,
        CancellationToken ct)
    {
        var respondentUserId = User.FindFirstValue("sub")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.Identity?.Name;

        var answers = await questionService.SubmitAnswersBulkAsync(dto, respondentUserId, ct);
        return Created($"api/v1/rentals/surveys/answers?rental_uuid={dto.RentalUuid}", answers);
    }

    [HttpGet("questions/{questionGuid:Guid}/answers")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> ListAnswersForQuestion(
        [FromRoute] Guid questionGuid,
        [FromQuery] Guid? rentalGuid,
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0,
        CancellationToken ct = default)
    {
        var (items, total) = await questionService.ListAnswersForQuestionAsync(questionGuid, rentalGuid, limit, offset, ct);
        return Ok(new { list = items, total });
    }

    [HttpGet("answers/{answerGuid:Guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public IActionResult GetAnswer([FromRoute] Guid answerGuid)
    {
        return NotFound();
    }

    [HttpDelete("answers/{answerGuid:Guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> DeleteAnswer(
        [FromRoute] Guid answerGuid,
        CancellationToken ct)
    {
        await questionService.DeleteAnswerAsync(answerGuid, ct);
        return NoContent();
    }
}
