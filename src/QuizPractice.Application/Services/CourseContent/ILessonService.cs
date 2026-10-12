using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.CourseContent;

namespace QuizPractice.Application.Services.CourseContent;

/// <summary>UC-49 View Subject Lessons, UC-50 Add or Edit Lesson, UC-51 Activate or Deactivate Lesson; Admin and the assigned Expert (BR-11).</summary>
public interface ILessonService
{
    Task<Result<LessonListDto>> ListAsync(int subjectId, LessonListRequest request, CancellationToken ct = default);

    /// <summary>Lesson types, topics and lesson quizzes of the subject for Lesson Details (UC-50).</summary>
    Task<Result<LessonChoicesDto>> GetChoicesAsync(int subjectId, CancellationToken ct = default);

    Task<Result<LessonDto>> GetAsync(int subjectId, int lessonId, CancellationToken ct = default);

    /// <summary>UC-50: stores the fields of the chosen type and clears the others.</summary>
    Task<Result<LessonDto>> CreateAsync(int subjectId, SaveLessonRequest request, CancellationToken ct = default);

    Task<Result<LessonDto>> UpdateAsync(int subjectId, int lessonId, SaveLessonRequest request, CancellationToken ct = default);

    Task<Result<LessonRowDto>> ChangeStatusAsync(int subjectId, int lessonId, ChangeLessonStatusRequest request, CancellationToken ct = default);
}
