using QuizPractice.Domain.Entities;

namespace QuizPractice.Domain.Repositories;

/// <summary>The lessons of a subject as Subject Lessons and Lesson Details manage them (UC-49 to UC-51).</summary>
public interface ILessonRepository
{
    /// <summary>Every lesson of the subject with its type, whatever its status; the caller orders them under their topic.</summary>
    Task<IReadOnlyList<Lesson>> ListAsync(int subjectId, CancellationToken ct = default);

    /// <summary>One lesson with its type, topic and quiz, tracked for a change.</summary>
    Task<Lesson?> GetByIdAsync(int id, CancellationToken ct = default);
}
