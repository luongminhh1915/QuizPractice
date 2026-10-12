using QuizPractice.Domain.Common;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Domain.Repositories;

/// <summary>Criteria of Practices List (UC-19): a subject, practices or exams, and the order of the date taken.</summary>
public sealed record AttemptSearch(int? SubjectId, AttemptMode? Mode, bool OldestFirst);

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

    /// <summary>UC-19: a page of the account's attempts with subject and quiz, the newest first unless asked otherwise.</summary>
    Task<Page<Attempt>> ListByUserAsync(int userId, AttemptSearch search, PageRequest page, CancellationToken ct = default);

    /// <summary>How many questions of each attempt were answered with the correct option (BR-21).</summary>
    Task<IReadOnlyDictionary<int, int>> GetCorrectCountsAsync(IReadOnlyCollection<int> attemptIds, CancellationToken ct = default);

    /// <summary>The subjects the account has taken an attempt in, by name (the Subject filter of Practices List).</summary>
    Task<IReadOnlyList<Subject>> ListTakenSubjectsAsync(int userId, CancellationToken ct = default);

    /// <summary>
    /// The account's simulation exams still in progress, tracked, with quiz, subject and the
    /// options of their questions, so one past its end time can be scored (UC-30 E1).
    /// </summary>
    Task<IReadOnlyList<Attempt>> ListOpenExamsAsync(int userId, CancellationToken ct = default);

    /// <summary>Stages a new attempt with its questions; the service commits through IUnitOfWork.</summary>
    void Add(Attempt attempt);
}
