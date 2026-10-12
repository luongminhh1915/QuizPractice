using QuizPractice.Domain.Entities;

namespace QuizPractice.Domain.Repositories;

/// <summary>Practices and simulation exams a customer takes (Customer UC-19 to UC-23, Learning UC-24 to UC-33).</summary>
public interface IAttemptRepository
{
    /// <summary>UC-20: every shown question of a subject, the pool of a practice over all groups.</summary>
    Task<IReadOnlyList<int>> GetShownQuestionIdsAsync(int subjectId, CancellationToken ct = default);

    /// <summary>
    /// One attempt of the account, tracked, with its subject, its quiz, and its questions
    /// (options, dimensions with their type) in display order; null when it is missing or
    /// belongs to someone else (UC-24 to UC-33).
    /// </summary>
    Task<Attempt?> GetOwnAsync(int id, int userId, CancellationToken ct = default);

    /// <summary>Names of topics (lessons) by id, for the criteria of a practice.</summary>
    Task<IReadOnlyDictionary<int, string>> GetLessonNamesAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default);

    /// <summary>Names of dimensions by id, as "Type: Name", for the criteria of a practice.</summary>
    Task<IReadOnlyDictionary<int, string>> GetDimensionNamesAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default);

    /// <summary>Stages a new attempt with its questions; the service commits through IUnitOfWork.</summary>
    void Add(Attempt attempt);
}
