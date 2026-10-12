using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizPractice.Application.Dtos.CourseContent;
using QuizPractice.Application.Services.CourseContent;
using QuizPractice.Domain.Constants;

namespace QuizPractice.Api.Controllers.CourseContent;

/// <summary>
/// Subject Lessons and Lesson Details (PDF II.6, F#28, F#29; UC-49 to UC-51). The lessons
/// belong to the subject, not to a package (OI-02). The Admin and the subject's Expert.
/// </summary>
[Route("api/subjects/{subjectId:int}/lessons")]
[Authorize(Policy = SettingValues.MenuItems.Subjects)]
public class LessonsController(ILessonService lessons) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<LessonListDto>> List(int subjectId, [FromQuery] LessonListRequest request, CancellationToken ct) =>
        FromResult(await lessons.ListAsync(subjectId, request, ct));

    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<LessonRowDto>> ChangeStatus(int subjectId, int id, ChangeLessonStatusRequest request, CancellationToken ct) =>
        FromResult(await lessons.ChangeStatusAsync(subjectId, id, request, ct));
}
