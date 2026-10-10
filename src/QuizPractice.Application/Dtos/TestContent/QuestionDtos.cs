using System.Security.Cryptography;
using System.Text;
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

/// <summary>
/// An answer option on Question Details. Id is empty for a new option; an existing one
/// keeps its id, so a stored attempt that chose it still points at it.
/// </summary>
public sealed record AnswerOptionInput(int? Id, string Content, bool IsCorrect);

/// <summary>
/// Question Details form (UC-53). The options are in the order the customer sees them.
/// A typed-in question starts Shown (SRS III 7.2). On edit, Version is the one the form
/// was loaded with: a different stored version means another user saved in between (E4).
/// </summary>
public sealed record SaveQuestionRequest(
    int SubjectId,
    int? LessonId,
    int LevelId,
    VisibilityStatus Status,
    IReadOnlyList<int> DimensionIds,
    string Content,
    string? Explanation,
    IReadOnlyList<AnswerOptionInput> Options,
    string? Version = null);

public sealed record AnswerOptionDto(int Id, string Content, bool IsCorrect)
{
    public static AnswerOptionDto From(AnswerOption o) => new(o.Id, o.Content, o.IsCorrect);
}

/// <summary>
/// A question as Question Details shows it. IsUsed: a quiz or a stored attempt has it,
/// so its subject cannot change and an option already chosen cannot be removed.
/// </summary>
public sealed record QuestionDto(
    int Id,
    int SubjectId,
    string Subject,
    int? LessonId,
    int LevelId,
    VisibilityStatus Status,
    IReadOnlyList<int> DimensionIds,
    string Content,
    string? Explanation,
    IReadOnlyList<AnswerOptionDto> Options,
    bool IsUsed,
    string Version)
{
    public static QuestionDto From(Question q, bool isUsed) =>
        new(q.Id, q.SubjectId, q.Subject.Name, q.LessonId, q.LevelId, q.Status,
            q.Dimensions.Select(d => d.DimensionId).Order().ToList(),
            q.Content, q.Explanation,
            q.Options.OrderBy(o => o.DisplayOrder).Select(AnswerOptionDto.From).ToList(),
            isUsed, QuestionVersion.Of(q));
}

/// <summary>
/// Version of a question for UC-53 E4. The Questions table has no updated_at or
/// rowversion column (SDS I.3), so the version is a fingerprint of everything the form
/// shows: any change by another user gives another value. Duck chose this on 10/10
/// over a schema change.
/// </summary>
public static class QuestionVersion
{
    public static string Of(Question q)
    {
        var parts = new List<string>
        {
            q.SubjectId.ToString(), q.LessonId?.ToString() ?? "", q.LevelId.ToString(), q.Status.ToString(),
            q.Content, q.Explanation ?? "", q.Media ?? "",
            string.Join(",", q.Dimensions.Select(d => d.DimensionId).Order()),
        };
        parts.AddRange(q.Options.OrderBy(o => o.DisplayOrder).Select(o => $"{o.Id}:{o.DisplayOrder}:{o.IsCorrect}:{o.Content}"));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\u001f', parts)));
        return Convert.ToHexString(hash, 0, 8);
    }
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
