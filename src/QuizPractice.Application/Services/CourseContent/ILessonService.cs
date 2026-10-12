using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.CourseContent;

namespace QuizPractice.Application.Services.CourseContent;

/// <summary>UC-49 View Subject Lessons; Admin and the assigned Expert (BR-11).</summary>
public interface ILessonService
{
    Task<Result<LessonListDto>> ListAsync(int subjectId, LessonListRequest request, CancellationToken ct = default);
}
