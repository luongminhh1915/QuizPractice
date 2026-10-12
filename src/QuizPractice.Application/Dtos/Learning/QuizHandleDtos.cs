using QuizPractice.Domain.Enums;

namespace QuizPractice.Application.Dtos.Learning;

/// <summary>One answer option as Quiz Handle shows it: lettered A, B, C… in display order. Never says which one is correct.</summary>
public sealed record HandleOptionDto(int Id, string Letter, string Content);

/// <summary>
/// One question of a running attempt (SRS III 4.1). MediaKind is image, audio or video when
/// the question carries a file, streamed by GET api/attempts/{id}/questions/{order}/media.
/// </summary>
public sealed record HandleQuestionDto(
    int Order,
    int QuestionId,
    string Content,
    string? MediaKind,
    IReadOnlyList<HandleOptionDto> Options,
    int? ChosenOptionId,
    bool IsMarked);

/// <summary>
/// Quiz Handle (SRS III 4.1; UC-24). The whole attempt at once, so moving between the
/// questions needs no call. EndsAt is set for a simulation exam only (BR-06); ServerNow
/// lets the page run the clock on the server's time.
/// </summary>
public sealed record QuizHandleDto(
    int Id,
    string SubjectName,
    string Title,
    AttemptMode Mode,
    DateTime StartedAt,
    DateTime? EndsAt,
    DateTime ServerNow,
    int CurrentOrder,
    IReadOnlyList<HandleQuestionDto> Questions);

/// <summary>UC-24 step 2: the option chosen for the question.</summary>
public sealed record AnswerRequest(int OptionId);

/// <summary>UC-25 step 4: the question now on screen, kept for a reload (BR-08).</summary>
public sealed record MoveRequest(int Order);

/// <summary>UC-26: mark the question for review, or unmark it (A1).</summary>
public sealed record MarkRequest(bool Marked);

/// <summary>
/// Peek at Answer (UC-28 step 3, D6): the correct option, the explanation where the
/// question has one, and its dimensions as "Type: Name".
/// </summary>
public sealed record PeekDto(int OptionId, string Letter, string Content, string? Explanation, IReadOnlyList<string> Dimensions);

/// <summary>UC-29 step 6: the result of the scored attempt (BR-21); Practice Details shows it next.</summary>
public sealed record AttemptResultDto(int Id, int Correct, int QuestionCount, decimal Score, byte PassRate, bool Passed);

/// <summary>What is stored for one question after a change.</summary>
public sealed record HandleProgressDto(int Order, int? ChosenOptionId, bool IsMarked);
