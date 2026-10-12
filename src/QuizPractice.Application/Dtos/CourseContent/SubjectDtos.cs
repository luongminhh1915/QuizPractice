using QuizPractice.Application.Common;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Application.Dtos.CourseContent;

/// <summary>A setting or a user offered in a drop-down: its id and what the screen shows.</summary>
public sealed record LookupDto(int Id, string Name);

/// <summary>
/// Criteria of Subject Management List (UC-41). SortBy: id, name, category, lessons, owner
/// or status. CategoryId: a Subject Category setting.
/// </summary>
public sealed record SubjectListRequest(
    string? Search = null,
    int? CategoryId = null,
    SubjectStatus? Status = null,
    string? SortBy = null,
    bool Descending = false,
    int Page = 1,
    int PageSize = 10);

/// <summary>A row of Subject Management List; Owner is null for a subject nobody prepares yet (UC-42 A2).</summary>
public sealed record SubjectSummaryDto(
    int Id,
    string Name,
    int CategoryId,
    string Category,
    int LessonCount,
    int? OwnerId,
    string? Owner,
    SubjectStatus Status)
{
    public static SubjectSummaryDto From(Subject s, int lessonCount) =>
        new(s.Id, s.Name, s.CategoryId, s.Category.Value, lessonCount, s.ExpertId, s.Expert?.FullName, s.Status);
}

/// <summary>Choices of the list filters: the subject categories (BR-16).</summary>
public sealed record SubjectFiltersDto(IReadOnlyList<LookupDto> Categories);

/// <summary>
/// New Subject (UC-42). The thumbnail is required; ExpertId may stay empty (A2). A new
/// subject is always Unpublished until the Admin publishes it (UC-45).
/// </summary>
public sealed record CreateSubjectRequest(
    string Name,
    int CategoryId,
    int? ExpertId,
    bool IsFeatured,
    int PassRate,
    string? Description,
    FileUpload? Thumbnail = null);

/// <summary>
/// Overview tab (UC-44): the general information the Admin and the assigned Expert may
/// change. Leaving Thumbnail empty keeps the stored one. ExpertId is the owner as the form
/// shows it: only the Admin may send another one (UC-43); null leaves the subject without owner.
/// Status likewise: only the Admin publishes or unpublishes (UC-45). ConfirmNoPackage is the
/// Admin's answer to the warning that a subject without an active package shows no price (E1).
/// </summary>
public sealed record UpdateSubjectRequest(
    string Name,
    string? Tagline,
    string? Brief,
    int CategoryId,
    bool IsFeatured,
    int PassRate,
    string? Description,
    int? ExpertId,
    SubjectStatus Status,
    bool ConfirmNoPackage = false,
    FileUpload? Thumbnail = null);

/// <summary>Everything Subject Management Details shows on the Overview tab (UC-43 to UC-45).</summary>
public sealed record SubjectDto(
    int Id,
    string Name,
    string? Tagline,
    string Brief,
    string Thumbnail,
    int CategoryId,
    string Category,
    bool IsFeatured,
    byte PassRate,
    SubjectStatus Status,
    int? OwnerId,
    string? Owner,
    string Description,
    bool HasActivePackage)
{
    public static SubjectDto From(Subject s, bool hasActivePackage) =>
        new(s.Id, s.Name, s.Tagline, s.Brief, s.Thumbnail, s.CategoryId, s.Category.Value, s.IsFeatured, s.PassRate,
            s.Status, s.ExpertId, s.Expert?.FullName, s.Description, hasActivePackage);
}

/// <summary>Drop-downs of New Subject and the Overview tab: subject categories, and for the Admin the Experts (BR-16).</summary>
public sealed record SubjectChoicesDto(IReadOnlyList<LookupDto> Categories, IReadOnlyList<LookupDto> Experts);
