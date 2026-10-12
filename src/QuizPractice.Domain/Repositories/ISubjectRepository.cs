using QuizPractice.Domain.Common;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Domain.Repositories;

/// <summary>Criteria of Subjects List (UC-12): text matched against the name, and a subject category.</summary>
public sealed record PublishedSubjectSearch(string? Text, int? CategoryId);

/// <summary>
/// Criteria of Subject Management List (UC-41). ExpertId limits the result to the subjects
/// of that Expert (BR-11); null for the Admin. SortBy: id, name, category, lessons, owner or status.
/// </summary>
public sealed record ManagedSubjectSearch(
    int? ExpertId, string? Text, int? CategoryId, SubjectStatus? Status, string? SortBy, bool Descending);

/// <summary>
/// Subjects as the public pages show them: published only (UC-09, UC-12 to UC-14); and as
/// Course Content manages them, whatever their status (UC-41 to UC-51).
/// </summary>
public interface ISubjectRepository
{
    /// <summary>UC-41: a page of subjects with their category and owner.</summary>
    Task<Page<Subject>> SearchManagedAsync(ManagedSubjectSearch search, PageRequest page, CancellationToken ct = default);

    /// <summary>How many lessons and quizzes (not topics) each of these subjects holds.</summary>
    Task<IReadOnlyDictionary<int, int>> CountLessonsAsync(IReadOnlyCollection<int> subjectIds, CancellationToken ct = default);

    /// <summary>The subject with its category and owner, whatever its status, tracked for a change (UC-42 to UC-51).</summary>
    Task<Subject?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>The subjects this user owns, tracked so their owner can be removed (UC-63 A3).</summary>
    Task<IReadOnlyList<Subject>> ListOwnedByAsync(int userId, CancellationToken ct = default);

    /// <summary>Stages a new subject; the service commits through IUnitOfWork.</summary>
    void Add(Subject subject);

    /// <summary>Whether a customer could buy the subject: it has an Active price package (UC-45 E1).</summary>
    Task<bool> HasActivePackageAsync(int subjectId, CancellationToken ct = default);

    /// <summary>Published subjects whose featured flag is on, most recently updated first, with their category and active packages.</summary>
    Task<IReadOnlyList<Subject>> ListFeaturedPublishedAsync(int take, CancellationToken ct = default);

    /// <summary>UC-13: the subject with its category and active packages, or null when it does not exist or is not published.</summary>
    Task<Subject?> GetPublishedAsync(int id, CancellationToken ct = default);

    /// <summary>UC-12: a page of published subjects matching the criteria, most recently updated first.</summary>
    Task<Page<Subject>> SearchPublishedAsync(PublishedSubjectSearch search, PageRequest page, CancellationToken ct = default);
}
