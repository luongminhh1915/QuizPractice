using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Application.Dtos.TestContent;

/// <summary>Criteria of Quizzes List (UC-56). SortBy: id, subject or level. TypeId: a Test Type setting.</summary>
public sealed record QuizListRequest(
    string? Search = null,
    int? SubjectId = null,
    int? TypeId = null,
    string? SortBy = null,
    bool Descending = false,
    int Page = 1,
    int PageSize = 10);

/// <summary>
/// A row of Quizzes List. IsTaken: an attempt exists, so the quiz can no longer be
/// edited or deleted (BR-13). Status is shown only when Inactive (Duck 10/10).
/// </summary>
public sealed record QuizSummaryDto(
    int Id,
    string Name,
    int SubjectId,
    string Subject,
    int LevelId,
    string Level,
    short QuestionCount,
    short DurationMin,
    byte PassRate,
    int TypeId,
    string Type,
    ActiveStatus Status,
    bool IsTaken)
{
    public static QuizSummaryDto From(Quiz q, bool isTaken) =>
        new(q.Id, q.Name, q.SubjectId, q.Subject.Name, q.LevelId, q.Level.Value, q.QuestionCount, q.DurationMin,
            q.PassRate, q.TypeId, q.Type.Value, q.Status, isTaken);
}

/// <summary>The choices of the Quizzes List filters: the subjects the user may reach (BR-11) and the quiz types.</summary>
public sealed record QuizFiltersDto(IReadOnlyList<QuestionLookupDto> Subjects, IReadOnlyList<QuestionLookupDto> Types);
