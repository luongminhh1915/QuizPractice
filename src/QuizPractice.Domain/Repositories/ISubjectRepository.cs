using QuizPractice.Domain.Entities;

namespace QuizPractice.Domain.Repositories;

/// <summary>Subjects as the public pages show them: published only (UC-09, UC-12 to UC-14).</summary>
public interface ISubjectRepository
{
    /// <summary>Published subjects whose featured flag is on, most recently updated first, with their category and active packages.</summary>
    Task<IReadOnlyList<Subject>> ListFeaturedPublishedAsync(int take, CancellationToken ct = default);
}
