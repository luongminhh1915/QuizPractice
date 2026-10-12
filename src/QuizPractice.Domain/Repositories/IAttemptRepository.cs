using QuizPractice.Domain.Entities;

namespace QuizPractice.Domain.Repositories;

/// <summary>Practices and simulation exams a customer takes (Customer UC-19 to UC-23, Learning UC-24 to UC-33).</summary>
public interface IAttemptRepository
{
    /// <summary>UC-20: every shown question of a subject, the pool of a practice over all groups.</summary>
    Task<IReadOnlyList<int>> GetShownQuestionIdsAsync(int subjectId, CancellationToken ct = default);

    /// <summary>Stages a new attempt with its questions; the service commits through IUnitOfWork.</summary>
    void Add(Attempt attempt);
}
