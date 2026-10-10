using Microsoft.AspNetCore.Mvc;
using QuizPractice.Application.Dtos.TestContent;
using QuizPractice.Application.Services.TestContent;
using QuizPractice.Domain.Common;

namespace QuizPractice.Api.Controllers.TestContent;

/// <summary>
/// Questions List (PDF II.7, F#30; UC-52, UC-54). The service already refuses every role but the
/// Admin and the Expert, and limits an Expert to the assigned subjects (BR-11).
/// TODO(auth): add the role attribute once cookie sign-in and the role table (BR-03, D23) are in place.
/// </summary>
[Route("api/questions")]
public class QuestionsController(IQuestionService questions) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Page<QuestionSummaryDto>>> List([FromQuery] QuestionListRequest request, CancellationToken ct) =>
        FromResult(await questions.ListAsync(request, ct));

    [HttpGet("filters")]
    public async Task<ActionResult<QuestionFiltersDto>> Filters([FromQuery] int? subjectId, CancellationToken ct) =>
        FromResult(await questions.GetFiltersAsync(subjectId, ct));

    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<QuestionSummaryDto>> ChangeStatus(int id, ChangeQuestionStatusRequest request, CancellationToken ct) =>
        FromResult(await questions.ChangeStatusAsync(id, request, ct));
}
