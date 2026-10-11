using QuizPractice.Domain.Common;
using QuizPractice.Domain.Entities;

namespace QuizPractice.Domain.Repositories;

/// <summary>Criteria of Subjects List (UC-12): text matched against the name, and a subject category.</summary>
public sealed record PublishedSubjectSearch(string? Text, int? CategoryId);

/// <summary>Subjects as the public pages show them: published only (UC-09, UC-12 to UC-14).</summary>
public interface ISubjectRepository
{
    /// <summary>Published subjects whose featured flag is on, most recently updated first, with their category and active packages.</summary>
    Task<IReadOnlyList<Subject>> ListFeaturedPublishedAsync(int take, CancellationToken ct = default);

    /// <summary>UC-13: the subject with its category and active packages, or null when it does not exist or is not published.</summary>
    Task<Subject?> GetPublishedAsync(int id, CancellationToken ct = default);

    /// <summary>UC-12: a page of published subjects matching the criteria, most recently updated first.</summary>
    Task<Page<Subject>> SearchPublishedAsync(PublishedSubjectSearch search, PageRequest page, CancellationToken ct = default);
}
