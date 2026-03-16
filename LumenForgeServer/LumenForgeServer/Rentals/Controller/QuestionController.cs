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
    public async Task<IActionResult> ListActiveQuestions(CancellationToken ct)
    {
        var questions = await questionService.ListActiveQuestionsAsync(ct);
        return Ok(questions);
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
        // Ensure the question UUID in route matches the payload
        if (questionGuid != dto.QuestionUuid)
        {
            return BadRequest(new { error = "Question UUID mismatch between route and payload." });
        }

        var respondentUserId = User.FindFirstValue("sub")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.Identity?.Name;

        var answer = await questionService.SubmitAnswerAsync(dto, respondentUserId, ct);
        return CreatedAtAction(nameof(GetAnswer), new { answerGuid = answer.Uuid }, answer);
    }

    [HttpGet("questions/{questionGuid:Guid}/answers")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> ListAnswersForQuestion(
        [FromRoute] Guid questionGuid,
        [FromQuery] Guid? rentalGuid,
        CancellationToken ct)
    {
        var answers = await questionService.ListAnswersForQuestionAsync(questionGuid, rentalGuid, ct);
        return Ok(answers);
    }

    [HttpGet("answers/{answerGuid:Guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public async Task<IActionResult> GetAnswer(
        [FromRoute] Guid answerGuid,
        CancellationToken ct)
    {
        var answers = await questionService.ListAnswersForQuestionAsync(Guid.Empty, null, ct);
        // Note: In a real scenario, you'd retrieve a single answer by ID
        // For now, returning 404 as this would need a dedicated repository method
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
