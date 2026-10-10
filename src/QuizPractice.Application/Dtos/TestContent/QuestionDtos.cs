using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Application.Dtos.TestContent;

/// <summary>
/// Criteria of Questions List (UC-52). SortBy: id, subject, level or status.
/// DimensionIds repeats in the query string (?dimensionIds=3&amp;dimensionIds=5); a question
/// matches when it tests at least one of them.
/// </summary>
public sealed record QuestionListRequest(
    string? Search = null,
    int? SubjectId = null,
    int? LessonId = null,
    int[]? DimensionIds = null,
    int? LevelId = null,
    VisibilityStatus? Status = null,
    string? SortBy = null,
    bool Descending = false,
    int Page = 1,
    int PageSize = 10);

/// <summary>Show or Hide Question (UC-54).</summary>
public sealed record ChangeQuestionStatusRequest(VisibilityStatus Status);

/// <summary>A row of Questions List: no options, media or explanation.</summary>
public sealed record QuestionSummaryDto(
    int Id,
    string Content,
    int SubjectId,
    string Subject,
    int? LessonId,
    string? Lesson,
    IReadOnlyList<string> Dimensions,
    int LevelId,
    string Level,
    VisibilityStatus Status)
{
    public static QuestionSummaryDto From(Question q) =>
        new(q.Id, q.Content, q.SubjectId, q.Subject.Name, q.LessonId, q.Lesson?.Name,
            q.Dimensions.Select(d => d.Dimension.Name).Order().ToList(),
            q.LevelId, q.Level.Value, q.Status);
}

/// <summary>An entry of a filter list: a subject or a level.</summary>
public sealed record QuestionLookupDto(int Id, string Name);

/// <summary>A lesson of the chosen subject; Topic groups the Lesson list.</summary>
public sealed record QuestionLessonDto(int Id, string Name, string Topic)
{
    public static QuestionLessonDto From(Lesson l) => new(l.Id, l.Name, l.Topic!.Name);
}

/// <summary>A dimension of the chosen subject; Type is the Dimension Type setting (Domain, Group…).</summary>
public sealed record QuestionDimensionDto(int Id, string Name, string Type)
{
    public static QuestionDimensionDto From(SubjectDimension d) => new(d.Id, d.Name, d.Type.Value);
}

/// <summary>
/// The choices of the Questions List filters (SRS III 7.1). Subjects are the ones the user
/// may reach (BR-11); Lessons and Dimensions follow the chosen subject and stay empty
/// until one is chosen.
/// </summary>
public sealed record QuestionFiltersDto(
    IReadOnlyList<QuestionLookupDto> Subjects,
    IReadOnlyList<QuestionLookupDto> Levels,
    IReadOnlyList<QuestionLessonDto> Lessons,
    IReadOnlyList<QuestionDimensionDto> Dimensions);
