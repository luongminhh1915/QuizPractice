using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Customer;

namespace QuizPractice.Application.Services.Customer;

/// <summary>
/// My Subjects (UC-71 Take Lesson Quiz): the subjects the customer may reach, their lessons,
/// and the start of a lesson quiz, which asks the fixed set the quiz drew at its last save.
/// </summary>
public interface IMySubjectService
{
    Task<Result<IReadOnlyList<MySubjectDto>>> ListAsync(CancellationToken ct = default);

    Task<Result<SubjectLessonsDto>> GetLessonsAsync(int subjectId, CancellationToken ct = default);

    Task<Result<AttemptCreatedDto>> StartQuizAsync(int subjectId, int lessonId, CancellationToken ct = default);
}
