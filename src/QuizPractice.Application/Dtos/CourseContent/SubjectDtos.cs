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
