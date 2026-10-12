using QuizPractice.Domain.Common;
using QuizPractice.Domain.Entities;

namespace QuizPractice.Domain.Repositories;

/// <summary>Registrations of subjects (UC-14 now; My Registrations and the Sale screens extend it).</summary>
public interface IRegistrationRepository
{
    /// <summary>UC-14 A3: the account's registration of the subject still in Submitted, with its package, if any.</summary>
    Task<Registration?> FindSubmittedAsync(int userId, int subjectId, CancellationToken ct = default);

    /// <summary>UC-15: a page of the account's registrations, the newest first, with subject, package and invoice.</summary>
    Task<Page<Registration>> ListByUserAsync(int userId, PageRequest page, CancellationToken ct = default);

    /// <summary>Stages a new registration; the service commits through IUnitOfWork.</summary>
    void Add(Registration registration);
}
