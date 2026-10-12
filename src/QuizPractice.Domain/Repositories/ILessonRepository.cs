using QuizPractice.Domain.Entities;

namespace QuizPractice.Domain.Repositories;

/// <summary>The lessons of a subject as Subject Lessons and Lesson Details manage them (UC-49 to UC-51).</summary>
public interface ILessonRepository
{
    /// <summary>Every lesson of the subject with its type, whatever its status; the caller orders them under their topic.</summary>
    Task<IReadOnlyList<Lesson>> ListAsync(int subjectId, CancellationToken ct = default);

    /// <summary>One lesson with its type, topic and quiz, tracked for a change.</summary>
    Task<Lesson?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>Whether lessons or quizzes sit under this topic (a topic that has them cannot change type).</summary>
    Task<bool> HasLessonsUnderAsync(int topicId, CancellationToken ct = default);

    /// <summary>The position after the last lesson under this topic, or after the last topic when topicId is null.</summary>
    Task<int> NextOrderAsync(int subjectId, int? topicId, CancellationToken ct = default);

    /// <summary>The lesson quizzes of a subject (Test Type "Lesson quiz"), whatever their status, by name (UC-50 A3).</summary>
    Task<IReadOnlyList<Quiz>> ListLessonQuizzesAsync(int subjectId, CancellationToken ct = default);

    void Add(Lesson lesson);
}
