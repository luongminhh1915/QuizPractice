using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizPractice.Api.Controllers.Marketing;
using QuizPractice.Application.Dtos.CourseContent;
using QuizPractice.Application.Services.CourseContent;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Constants;

namespace QuizPractice.Api.Controllers.CourseContent;

/// <summary>
/// Subject Management List and Details (PDF II.6, F#23 to F#25; UC-41 to UC-45). The service
/// limits an Expert to the assigned subjects (BR-11) and keeps the Admin-only actions for the Admin (BR-12).
/// </summary>
[Route("api/subjects")]
[Authorize(Policy = SettingValues.MenuItems.Subjects)]
public class SubjectsController(ISubjectService subjects) : ApiControllerBase
{
    private const long FormLimit = FieldLength.ImageMaxBytes + 4 * FieldLength.LongText + 64 * 1024;

    [HttpGet]
    public async Task<ActionResult<Page<SubjectSummaryDto>>> List([FromQuery] SubjectListRequest request, CancellationToken ct) =>
        FromResult(await subjects.ListAsync(request, ct));

    [HttpGet("filters")]
    public async Task<ActionResult<SubjectFiltersDto>> Filters(CancellationToken ct) =>
        FromResult(await subjects.GetFiltersAsync(ct));

    /// <summary>Subject categories, and for the Admin the Experts who may own a subject.</summary>
    [HttpGet("choices")]
    public async Task<ActionResult<SubjectChoicesDto>> Choices(CancellationToken ct) =>
        FromResult(await subjects.GetChoicesAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SubjectDto>> Get(int id, CancellationToken ct) =>
        FromResult(await subjects.GetAsync(id, ct));

    /// <summary>UC-42, Admin only: 403 for an Expert.</summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(FormLimit)]
    public async Task<ActionResult<SubjectDto>> Create([FromForm] NewSubjectForm form, CancellationToken ct)
    {
        var result = await subjects.CreateAsync(await form.ToRequestAsync(ct), ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value.Id }, result.Value)
            : Problem(result.Error!);
    }

    /// <summary>UC-44: the Admin or the assigned Expert (403 for another Expert, E1).</summary>
    [HttpPut("{id:int}")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(FormLimit)]
    public async Task<ActionResult<SubjectDto>> Update(int id, [FromForm] SubjectOverviewForm form, CancellationToken ct) =>
        FromResult(await subjects.UpdateAsync(id, await form.ToRequestAsync(ct), ct));
}

/// <summary>Transport shape of the Overview tab; the rules live on UpdateSubjectRequest.</summary>
public sealed class SubjectOverviewForm
{
    public string Name { get; set; } = string.Empty;
    public string? Tagline { get; set; }
    public string? Brief { get; set; }
    public int CategoryId { get; set; }
    public bool IsFeatured { get; set; }
    public int PassRate { get; set; }
    public string? Description { get; set; }
    public IFormFile? Thumbnail { get; set; }

    public async Task<UpdateSubjectRequest> ToRequestAsync(CancellationToken ct) =>
        new(Name, Tagline, Brief, CategoryId, IsFeatured, PassRate, Description,
            Thumbnail is null ? null : await FormFiles.BufferAsync(Thumbnail, ct));
}

/// <summary>Transport shape of New Subject (IFormFile stays in the API); the rules live on CreateSubjectRequest.</summary>
public sealed class NewSubjectForm
{
    public string Name { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public int? ExpertId { get; set; }
    public bool IsFeatured { get; set; }
    public int PassRate { get; set; }
    public string? Description { get; set; }
    public IFormFile? Thumbnail { get; set; }

    public async Task<CreateSubjectRequest> ToRequestAsync(CancellationToken ct) =>
        new(Name, CategoryId, ExpertId, IsFeatured, PassRate, Description,
            Thumbnail is null ? null : await FormFiles.BufferAsync(Thumbnail, ct));
}
