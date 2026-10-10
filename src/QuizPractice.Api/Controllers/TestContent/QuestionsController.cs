using Microsoft.AspNetCore.Mvc;
using QuizPractice.Application.Dtos.TestContent;
using QuizPractice.Application.Services.TestContent;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Api.Controllers.TestContent;

/// <summary>
/// Questions List and Question Details (PDF II.7, F#30, F#31; UC-52 to UC-54). The service already refuses every role but the
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

    [HttpGet("{id:int}")]
    public async Task<ActionResult<QuestionDto>> Get(int id, CancellationToken ct) =>
        FromResult(await questions.GetAsync(id, ct));

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<QuestionDto>> Create([FromForm] QuestionForm form, CancellationToken ct)
    {
        var result = await questions.CreateAsync(form.ToRequest(), ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value.Id }, result.Value)
            : Problem(result.Error!);
    }

    [HttpPut("{id:int}")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<QuestionDto>> Update(int id, [FromForm] QuestionForm form, CancellationToken ct) =>
        FromResult(await questions.UpdateAsync(id, form.ToRequest(), ct));

    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<QuestionSummaryDto>> ChangeStatus(int id, ChangeQuestionStatusRequest request, CancellationToken ct) =>
        FromResult(await questions.ChangeStatusAsync(id, request, ct));
}

/// <summary>
/// Transport shape of the Question Details form, sent as multipart so the media file can
/// travel with it: options[0].content, options[0].isCorrect, options[0].id, dimensionIds…
/// The rules live on SaveQuestionRequest.
/// </summary>
public sealed class QuestionForm
{
    public int SubjectId { get; set; }
    public int? LessonId { get; set; }
    public int LevelId { get; set; }
    public VisibilityStatus Status { get; set; } = VisibilityStatus.Shown;
    public List<int> DimensionIds { get; set; } = [];
    public string? Content { get; set; }
    public string? Explanation { get; set; }
    public List<AnswerOptionForm> Options { get; set; } = [];
    public string? Version { get; set; }

    public SaveQuestionRequest ToRequest() =>
        new(SubjectId, LessonId, LevelId, Status, DimensionIds, Content ?? string.Empty, Explanation,
            Options.Select(o => new AnswerOptionInput(o.Id, o.Content ?? string.Empty, o.IsCorrect)).ToList(), Version);
}

public sealed class AnswerOptionForm
{
    public int? Id { get; set; }
    public string? Content { get; set; }
    public bool IsCorrect { get; set; }
}
