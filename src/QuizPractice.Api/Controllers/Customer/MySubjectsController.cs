using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizPractice.Application.Dtos.Customer;
using QuizPractice.Application.Services.Customer;
using QuizPractice.Domain.Constants;

namespace QuizPractice.Api.Controllers.Customer;

/// <summary>My Subjects (UC-71 Take Lesson Quiz): the lessons of the subjects the customer may reach.</summary>
[Route("api/my/subjects")]
[Authorize(Policy = SettingValues.MenuItems.MySubjects)]
public class MySubjectsController(IMySubjectService subjects) : ApiControllerBase
{
    /// <summary>The subjects a Paid registration covers today; empty when there is none.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MySubjectDto>>> List(CancellationToken ct) => FromResult(await subjects.ListAsync(ct));

    /// <summary>The Active lessons of the subject under their topics; 403 Subject.NoAccess (E1).</summary>
    [HttpGet("{id:int}/lessons")]
    public async Task<ActionResult<SubjectLessonsDto>> Lessons(int id, CancellationToken ct) =>
        FromResult(await subjects.GetLessonsAsync(id, ct));

    /// <summary>
    /// 201 with the attempt at the lesson quiz; 403 Subject.NoAccess (E1), 404 for a lesson that
    /// is not an Active Quiz lesson of the subject, 409 LessonQuiz.NotAvailable (E2) or LessonQuiz.Unfinished (A1).
    /// </summary>
    [HttpPost("{id:int}/lessons/{lessonId:int}/start")]
    public async Task<ActionResult<AttemptCreatedDto>> Start(int id, int lessonId, CancellationToken ct)
    {
        var result = await subjects.StartQuizAsync(id, lessonId, ct);
        return result.IsSuccess ? StatusCode(StatusCodes.Status201Created, result.Value) : Problem(result.Error!);
    }
}
