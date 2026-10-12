using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Application.Dtos.Sale;

/// <summary>
/// Criteria of Registrations List (UC-59). From and To are Vietnam dates of the
/// registration time, both included. SortBy is one of the nine columns of SRS III 8.1.
/// </summary>
public sealed record RegistrationListRequest(
    string? Search = null,
    int? SubjectId = null,
    DateOnly? From = null,
    DateOnly? To = null,
    RegistrationStatus? Status = null,
    string? SortBy = null,
    bool Descending = false,
    int Page = 1,
    int PageSize = 10);

/// <summary>One row of Registrations List (SRS III 8.1).</summary>
public sealed record RegistrationSummaryDto(
    int Id,
    string Email,
    DateTime RegisteredAt,
    string SubjectName,
    string PackageName,
    decimal TotalCost,
    RegistrationStatus Status,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    string UpdatedBy)
{
    public static RegistrationSummaryDto From(Registration r) =>
        new(r.Id, r.Email, DateTime.SpecifyKind(r.RegisteredAt, DateTimeKind.Utc), r.Subject.Name, r.Package.Name, r.TotalCost,
            r.Status, r.ValidFrom, r.ValidTo, r.UpdatedBy.Email);
}

/// <summary>A subject to filter by or to register for.</summary>
public sealed record SaleSubjectDto(int Id, string Name);

/// <summary>The Subject filter of Registrations List: every subject, by name.</summary>
public sealed record RegistrationFiltersDto(IReadOnlyList<SaleSubjectDto> Subjects);
