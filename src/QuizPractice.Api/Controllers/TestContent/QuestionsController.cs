using Microsoft.AspNetCore.Mvc;
using QuizPractice.Api.Controllers.Marketing;
using QuizPractice.Application.Dtos.TestContent;
using QuizPractice.Application.Services.TestContent;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Constants;
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
    /// <summary>The largest media file (an MP4, UC-53 E3) plus the text of the form.</summary>
    private const long FormLimit = FieldLength.VideoMaxBytes + 512 * 1024;

    [HttpGet]
    public async Task<ActionResult<Page<QuestionSummaryDto>>> List([FromQuery] QuestionListRequest request, CancellationToken ct) =>
        FromResult(await questions.ListAsync(request, ct));

    [HttpGet("filters")]
    public async Task<ActionResult<QuestionFiltersDto>> Filters([FromQuery] int? subjectId, CancellationToken ct) =>
        FromResult(await questions.GetFiltersAsync(subjectId, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<QuestionDto>> Get(int id, CancellationToken ct) =>
        FromResult(await questions.GetAsync(id, ct));

    /// <summary>
    /// Streams the media of a question to a user who may open the question (BR-05, BR-11).
    /// Range requests let a video or audio player seek.
    /// </summary>
    [HttpGet("{id:int}/media")]
    public async Task<IActionResult> Media(int id, CancellationToken ct)
    {
        var result = await questions.GetMediaAsync(id, ct);
        if (!result.IsSuccess)
        {
            return Problem(result.Error!);
        }

        // The type comes from our own list; the browser must not guess another one.
        Response.Headers.XContentTypeOptions = "nosniff";
        Response.Headers.CacheControl = "private, max-age=3600";
        return File(result.Value.Content, result.Value.ContentType, enableRangeProcessing: true);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(FormLimit)]
    public async Task<ActionResult<QuestionDto>> Create([FromForm] QuestionForm form, CancellationToken ct)
    {
        var result = await questions.CreateAsync(await form.ToRequestAsync(ct), ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value.Id }, result.Value)
            : Problem(result.Error!);
    }

    [HttpPut("{id:int}")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(FormLimit)]
    public async Task<ActionResult<QuestionDto>> Update(int id, [FromForm] QuestionForm form, CancellationToken ct) =>
        FromResult(await questions.UpdateAsync(id, await form.ToRequestAsync(ct), ct));

    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<QuestionSummaryDto>> ChangeStatus(int id, ChangeQuestionStatusRequest request, CancellationToken ct) =>
        FromResult(await questions.ChangeStatusAsync(id, request, ct));
}

/// <summary>
/// Transport shape of the Question Details form, sent as multipart so the media file can
/// travel with it: options[0].content, options[0].isCorrect, options[0].id, dimensionIds, media…
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
    public IFormFile? Media { get; set; }
    public bool RemoveMedia { get; set; }

    public async Task<SaveQuestionRequest> ToRequestAsync(CancellationToken ct) =>
        new(SubjectId, LessonId, LevelId, Status, DimensionIds, Content ?? string.Empty, Explanation,
            Options.Select(o => new AnswerOptionInput(o.Id, o.Content ?? string.Empty, o.IsCorrect)).ToList(), Version,
            Media is null ? null : await FormFiles.BufferAsync(Media, ct), RemoveMedia);
}

public sealed class AnswerOptionForm
{
    public int? Id { get; set; }
    public string? Content { get; set; }
    public bool IsCorrect { get; set; }
}
