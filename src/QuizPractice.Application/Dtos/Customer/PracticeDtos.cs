using QuizPractice.Domain.Enums;

namespace QuizPractice.Application.Dtos.Customer;

/// <summary>A subject the customer may practise (BR-05).</summary>
public sealed record PracticeSubjectDto(int Id, string Name);

/// <summary>
/// Practice Details, new practice (SRS III 3.3; UC-20 step 2). An empty list is E1: the
/// customer has no subject with valid access, and the screen offers Subjects List.
/// </summary>
public sealed record PracticeFormDto(IReadOnlyList<PracticeSubjectDto> Subjects);

/// <summary>One topic or one dimension of the subject and the shown questions it holds. Type is the dimension type, empty for a topic.</summary>
public sealed record PracticeGroupDto(int Id, string Name, string? Type, int Available);

/// <summary>The groups of one way of selecting (Question Group) and the shown questions of the whole subject (all groups).</summary>
public sealed record PracticeGroupsDto(int Total, IReadOnlyList<PracticeGroupDto> Groups);

/// <summary>UC-20 step 3. GroupId empty = all topics, or all dimensions.</summary>
public sealed record CreatePracticeRequest(int SubjectId, int QuestionCount, SelectionType SelectionType, int? GroupId);

/// <summary>
/// Result of a scored attempt (BR-21): correct answers out of the questions, the
/// percentage, the pass rate it was judged against, and the time it took.
/// </summary>
public sealed record AttemptOutcomeDto(int Correct, decimal Score, byte PassRate, bool Passed, int DurationSeconds);

/// <summary>
/// Practice Details of an existing attempt (SRS III 3.3; UC-21), read only: a practice
/// with its criteria, or a simulation exam with its name. Outcome is empty while the
/// attempt still runs; the page then offers to continue it on Quiz Handle.
/// </summary>
public sealed record PracticeDetailsDto(
    int Id,
    string SubjectName,
    AttemptMode Mode,
    string Title,
    SelectionType? SelectionType,
    string? GroupName,
    int QuestionCount,
    AttemptStatus Status,
    DateTime StartedAt,
    AttemptOutcomeDto? Outcome);

/// <summary>The attempt just made; Quiz Handle opens it.</summary>
public sealed record AttemptCreatedDto(int Id);
