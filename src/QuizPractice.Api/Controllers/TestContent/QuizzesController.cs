using Microsoft.AspNetCore.Mvc;
using QuizPractice.Application.Dtos.TestContent;
using QuizPractice.Application.Services.TestContent;
using QuizPractice.Domain.Common;

namespace QuizPractice.Api.Controllers.TestContent;

/// <summary>
/// Quizzes List (PDF II.7, F#33; UC-56, UC-58). The service already refuses every role but the
/// Admin and the Expert, and limits an Expert to the assigned subjects (BR-11).
/// TODO(auth): add the role attribute once cookie sign-in and the role table (BR-03, D23) are in place.
/// </summary>
[Route("api/quizzes")]
public class QuizzesController(IQuizService quizzes) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Page<QuizSummaryDto>>> List([FromQuery] QuizListRequest request, CancellationToken ct) =>
        FromResult(await quizzes.ListAsync(request, ct));

    [HttpGet("filters")]
    public async Task<ActionResult<QuizFiltersDto>> Filters(CancellationToken ct) =>
        FromResult(await quizzes.GetFiltersAsync(ct));

    /// <summary>204 when removed; 409 when taken (E2) or opened by a lesson (E3).</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await quizzes.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : Problem(result.Error!);
    }
}
