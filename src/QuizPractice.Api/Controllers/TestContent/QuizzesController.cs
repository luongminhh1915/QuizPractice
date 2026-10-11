using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizPractice.Application.Dtos.TestContent;
using QuizPractice.Application.Services.TestContent;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Constants;

namespace QuizPractice.Api.Controllers.TestContent;

/// <summary>
/// Quizzes List and Quiz Details (PDF II.7, F#33, F#34; UC-56 to UC-58). The service already refuses every role but the
/// Admin and the Expert, and limits an Expert to the assigned subjects (BR-11).
/// </summary>
[Route("api/quizzes")]
[Authorize(Policy = SettingValues.MenuItems.Quizzes)]
public class QuizzesController(IQuizService quizzes) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Page<QuizSummaryDto>>> List([FromQuery] QuizListRequest request, CancellationToken ct) =>
        FromResult(await quizzes.ListAsync(request, ct));

    [HttpGet("filters")]
    public async Task<ActionResult<QuizFiltersDto>> Filters(CancellationToken ct) =>
        FromResult(await quizzes.GetFiltersAsync(ct));

    /// <summary>Subjects (with their pass rate), levels and quiz types of the Overview tab.</summary>
    [HttpGet("choices")]
    public async Task<ActionResult<QuizChoicesDto>> Choices(CancellationToken ct) =>
        FromResult(await quizzes.GetChoicesAsync(ct));

    /// <summary>The ways a subject's questions can be drawn and the groups of one of them (Setting tab).</summary>
    [HttpGet("groups")]
    public async Task<ActionResult<QuizGroupsDto>> Groups([FromQuery] int subjectId, [FromQuery] string? drawnBy, CancellationToken ct) =>
        FromResult(await quizzes.GetGroupsAsync(subjectId, drawnBy, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<QuizDto>> Get(int id, CancellationToken ct) =>
        FromResult(await quizzes.GetAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<QuizDto>> Create(SaveQuizRequest request, CancellationToken ct)
    {
        var result = await quizzes.CreateAsync(request, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value.Id }, result.Value)
            : Problem(result.Error!);
    }

    /// <summary>409 when the quiz has been taken (A2, E5): only PUT status then.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<QuizDto>> Update(int id, SaveQuizRequest request, CancellationToken ct) =>
        FromResult(await quizzes.UpdateAsync(id, request, ct));

    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<QuizDto>> ChangeStatus(int id, ChangeQuizStatusRequest request, CancellationToken ct) =>
        FromResult(await quizzes.ChangeStatusAsync(id, request, ct));

    /// <summary>204 when removed; 409 when taken (E2) or opened by a lesson (E3).</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await quizzes.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : Problem(result.Error!);
    }
}
