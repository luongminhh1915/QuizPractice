using QuizPractice.Domain.Enums;

namespace QuizPractice.Application.Dtos.Learning;

/// <summary>One option of a reviewed question, now with whether it is the correct one.</summary>
public sealed record ReviewOptionDto(int Id, string Letter, string Content, bool IsCorrect);

/// <summary>
/// One question of a scored attempt (SRS III 4.2): the answer given, the correct one,
/// whether it was right (an unanswered question counts as wrong, BR-21), and the
/// explanation, which the page offers only when it is not empty (BR-10).
/// </summary>
public sealed record ReviewQuestionDto(
    int Order,
    int QuestionId,
    string Content,
    string? MediaKind,
    IReadOnlyList<ReviewOptionDto> Options,
    int? ChosenOptionId,
    bool IsMarked,
    bool IsCorrect,
    string? Explanation);

/// <summary>Quiz Review (SRS III 4.2; UC-31): the result bar and every question of a scored attempt.</summary>
public sealed record QuizReviewDto(
    int Id,
    string SubjectName,
    string Title,
    AttemptMode Mode,
    AttemptResultDto Result,
    IReadOnlyList<ReviewQuestionDto> Questions);
