using QuizPractice.Domain.Common;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Domain.Repositories;

/// <summary>
/// Criteria of Registrations List (UC-59): text matched against the email, one subject, a
/// registration-time range in UTC (From included, Before excluded) and one status. SortBy
/// is one of id, email, registeredAt, subject, package, totalCost, status, validFrom, validTo.
/// </summary>
public sealed record RegistrationSearch(
    string? Text,
    int? SubjectId,
    DateTime? From,
    DateTime? Before,
    RegistrationStatus? Status,
    string? SortBy,
    bool Descending);

/// <summary>Registrations of subjects (UC-14 now; My Registrations and the Sale screens extend it).</summary>
public interface IRegistrationRepository
{
    /// <summary>UC-14 A3: the account's registration of the subject still in Submitted, with its package, if any.</summary>
    Task<Registration?> FindSubmittedAsync(int userId, int subjectId, CancellationToken ct = default);

    /// <summary>UC-15: a page of the account's registrations, the newest first, with subject, package and invoice.</summary>
    Task<Page<Registration>> ListByUserAsync(int userId, PageRequest page, CancellationToken ct = default);

    /// <summary>
    /// UC-16, UC-17: one registration of the account, tracked, with subject (and its
    /// packages), package and invoice; null when it is missing or belongs to someone else.
    /// </summary>
    Task<Registration?> GetOwnAsync(int id, int userId, CancellationToken ct = default);

    /// <summary>UC-18: the account's registrations among the ids, tracked, with subject, package and invoice.</summary>
    Task<IReadOnlyList<Registration>> ListOwnByIdsAsync(int userId, IReadOnlyCollection<int> ids, CancellationToken ct = default);

    /// <summary>
    /// BR-05: the subjects the account may practise on that day — a Paid registration whose
    /// validity (valid_from to valid_to, both included) covers it — by name.
    /// </summary>
    Task<IReadOnlyList<Subject>> ListAccessibleSubjectsAsync(int userId, DateOnly day, CancellationToken ct = default);

    /// <summary>UC-59: a page of every registration with subject, package and the user who last changed it.</summary>
    Task<Page<Registration>> SearchAsync(RegistrationSearch search, PageRequest page, CancellationToken ct = default);

    /// <summary>Stages a new registration; the service commits through IUnitOfWork.</summary>
    void Add(Registration registration);
}
