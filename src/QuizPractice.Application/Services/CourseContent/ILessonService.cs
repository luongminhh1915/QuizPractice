using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.CourseContent;

namespace QuizPractice.Application.Services.CourseContent;

/// <summary>UC-49 View Subject Lessons, UC-51 Activate or Deactivate Lesson; Admin and the assigned Expert (BR-11).</summary>
public interface ILessonService
{
    Task<Result<LessonListDto>> ListAsync(int subjectId, LessonListRequest request, CancellationToken ct = default);

    Task<Result<LessonRowDto>> ChangeStatusAsync(int subjectId, int lessonId, ChangeLessonStatusRequest request, CancellationToken ct = default);
}
