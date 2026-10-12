using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    [HttpGet]
    public async Task<ActionResult<Page<SubjectSummaryDto>>> List([FromQuery] SubjectListRequest request, CancellationToken ct) =>
        FromResult(await subjects.ListAsync(request, ct));

    [HttpGet("filters")]
    public async Task<ActionResult<SubjectFiltersDto>> Filters(CancellationToken ct) =>
        FromResult(await subjects.GetFiltersAsync(ct));
}
