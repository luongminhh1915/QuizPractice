using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Domain.Repositories;

/// <summary>When a registration was made and where it stands now (UC-34 counts and trend).</summary>
public sealed record RegistrationPoint(DateTime RegisteredAt, RegistrationStatus Status);

/// <summary>A Paid registration: the day it became Paid (BR-14), its cost and its subject's category.</summary>
public sealed record PaidRegistration(DateOnly PaidOn, decimal TotalCost, int CategoryId);

/// <summary>
/// The figures of the Management Dashboard (UC-34). Times are UTC, [fromUtc, toUtc) a half-open
/// range; days are DateOnly, [from, to] inclusive. The service turns Vietnam days into both.
/// </summary>
public interface IDashboardRepository
{
    /// <summary>Subjects created in the range, and all subjects.</summary>
    Task<(int Created, int Total)> CountSubjectsAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);

    /// <summary>The registrations made in the range, with their current status.</summary>
    Task<IReadOnlyList<RegistrationPoint>> GetRegistrationsAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);

    /// <summary>The registrations that became Paid on a day of the range.</summary>
    Task<IReadOnlyList<PaidRegistration>> GetPaidAsync(DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>Customer accounts created in the range.</summary>
    Task<int> CountNewCustomersAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);

    /// <summary>Customers whose first Paid registration falls on a day of the range.</summary>
    Task<int> CountFirstTimeBuyersAsync(DateOnly from, DateOnly to, CancellationToken ct = default);

    /// <summary>The Subject Category settings in their display order (BR-16).</summary>
    Task<IReadOnlyList<Setting>> GetCategoriesAsync(CancellationToken ct = default);
}
