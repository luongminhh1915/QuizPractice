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

/// <summary>An active price package with its prices (UC-60 step 4).</summary>
public sealed record SalePackageDto(int Id, string Name, int AccessMonths, decimal ListPrice, decimal SalePrice)
{
    public static SalePackageDto From(PricePackage p) => new(p.Id, p.Name, p.AccessMonths, p.ListPrice, p.SalePrice);
}

/// <summary>A subject on sale and its active packages, the cheapest first.</summary>
public sealed record SaleSubjectOptionDto(int Id, string Name, IReadOnlyList<SalePackageDto> Packages);

/// <summary>Registration Details, new registration: the subjects on sale (published, with an active package).</summary>
public sealed record RegistrationChoicesDto(IReadOnlyList<SaleSubjectOptionDto> Subjects);

/// <summary>The invoice a registration is paid in (BR-24).</summary>
public sealed record RegistrationInvoiceDto(int Id, string Code, InvoiceStatus Status, decimal TotalAmount);

/// <summary>
/// Registration Details (SRS III 8.2; UC-60, UC-61). RecordedBy is the Sale user who
/// recorded it, empty for an online registration. CanEditAll: this Sale user recorded it
/// and it is still Submitted and in no unpaid invoice (BR-22, BR-24). CanSettle: the
/// status may still change here (Submitted, in no unpaid invoice).
/// </summary>
public sealed record RegistrationDetailsDto(
    int Id,
    int SubjectId,
    string SubjectName,
    SalePackageDto Package,
    string FullName,
    Gender Gender,
    string Email,
    string Mobile,
    decimal TotalCost,
    RegistrationStatus Status,
    DateTime RegisteredAt,
    string? RecordedBy,
    string UpdatedBy,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    string? Notes,
    RegistrationInvoiceDto? Invoice,
    bool HasAccount,
    bool CanEditAll,
    bool CanSettle);

/// <summary>UC-60 steps 4 and 5: what the Sale user enters.</summary>
public sealed record SaveRegistrationRequest(
    int SubjectId,
    int PackageId,
    string FullName,
    Gender? Gender,
    string Email,
    string Mobile,
    string? Notes);

/// <summary>BR-22: the notes can be added at any status.</summary>
public sealed record SaveNotesRequest(string? Notes);
