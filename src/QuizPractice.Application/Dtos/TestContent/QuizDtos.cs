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

/// <summary>How many questions to draw from one group: a topic or a dimension (UC-57 step 5).</summary>
public sealed record QuizRuleInput(int GroupId, int QuestionCount);

/// <summary>
/// Quiz Details form (UC-57): the Overview tab, then the Setting tab. SelectionType is
/// "Topic" or a dimension type of the subject (QuizSelection); the rule counts add up to
/// QuestionCount. A new quiz starts Active.
/// </summary>
public sealed record SaveQuizRequest(
    string Name,
    int SubjectId,
    int LevelId,
    int TypeId,
    int DurationMin,
    int PassRate,
    string? Description,
    int QuestionCount,
    string SelectionType,
    IReadOnlyList<QuizRuleInput> Rules,
    ActiveStatus Status = ActiveStatus.Active);

/// <summary>The one change a taken quiz still accepts (UC-57 A2, BR-13, BR-23).</summary>
public sealed record ChangeQuizStatusRequest(ActiveStatus Status);

/// <summary>A group the quiz draws from: its name and how many shown questions it holds now.</summary>
public sealed record QuizGroupDto(int Id, string Name, int Available);

/// <summary>A rule of a stored quiz, with the group's current number of shown questions (E2).</summary>
public sealed record QuizRuleDto(int GroupId, string Group, int QuestionCount, int Available);

/// <summary>
/// A quiz as Quiz Details shows it. IsTaken: an attempt exists, so only the status may
/// change (A2). FixedQuestionCount: the set a lesson quiz drew at its last save.
/// </summary>
public sealed record QuizDto(
    int Id,
    string Name,
    int SubjectId,
    string Subject,
    int LevelId,
    int TypeId,
    string Type,
    short DurationMin,
    byte PassRate,
    ActiveStatus Status,
    string? Description,
    short QuestionCount,
    string SelectionType,
    IReadOnlyList<QuizRuleDto> Rules,
    int FixedQuestionCount,
    bool IsTaken);

/// <summary>A subject the user may reach, with the pass rate a new quiz starts from (BR-21).</summary>
public sealed record QuizSubjectDto(int Id, string Name, byte PassRate);

/// <summary>The choices of the Overview tab: subjects, question levels and quiz types (BR-16).</summary>
public sealed record QuizChoicesDto(
    IReadOnlyList<QuizSubjectDto> Subjects,
    IReadOnlyList<QuestionLookupDto> Levels,
    IReadOnlyList<QuestionLookupDto> Types);

/// <summary>
/// The choices of the Setting tab for one subject: "Topic" and its dimension types, and the
/// groups of the chosen way with their shown questions.
/// </summary>
public sealed record QuizGroupsDto(IReadOnlyList<string> DrawnBy, IReadOnlyList<QuizGroupDto> Groups);

/// <summary>The choices of the Quizzes List filters: the subjects the user may reach (BR-11) and the quiz types.</summary>
public sealed record QuizFiltersDto(IReadOnlyList<QuestionLookupDto> Subjects, IReadOnlyList<QuestionLookupDto> Types);
