using QuizPractice.Domain.Common;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Application.Dtos.CourseContent;

/// <summary>Criteria of Subject Lessons (UC-49): a topic (with the lessons under it), a status, a name.</summary>
public sealed record LessonListRequest(
    string? Search = null,
    int? TopicId = null,
    ActiveStatus? Status = null,
    int Page = 1,
    int PageSize = 10);

/// <summary>
/// A row of Subject Lessons. A topic row has no TopicId; a lesson or a quiz carries the
/// topic it sits under and is listed after that topic, indented (SRS III 6.6).
/// </summary>
public sealed record LessonRowDto(
    int Id,
    string Name,
    int DisplayOrder,
    string Type,
    bool IsTopic,
    int? TopicId,
    ActiveStatus Status);

/// <summary>Subject Lessons: the subject it belongs to (OI-02), its topics for the filter, and one page of rows.</summary>
public sealed record LessonListDto(
    int SubjectId,
    string Subject,
    IReadOnlyList<LookupDto> Topics,
    Page<LessonRowDto> Lessons);

/// <summary>Activate or Deactivate Lesson (UC-51).</summary>
public sealed record ChangeLessonStatusRequest(ActiveStatus Status);

/// <summary>
/// Lesson Details (UC-50). Which fields count depends on the type (F#29): a Subject Topic
/// has no topic, video or content; a Lesson has a topic, a video link and HTML content; a
/// Quiz has a topic and a lesson quiz of the subject. DisplayOrder empty: after the last one.
/// </summary>
public sealed record SaveLessonRequest(
    string Name,
    int TypeId,
    int? TopicId,
    int? DisplayOrder,
    string? VideoUrl,
    string? HtmlContent,
    int? QuizId);

public sealed record LessonDto(
    int Id,
    string Name,
    int TypeId,
    string Type,
    int? TopicId,
    int DisplayOrder,
    string? VideoUrl,
    string? HtmlContent,
    int? QuizId,
    ActiveStatus Status)
{
    public static LessonDto From(Lesson l) =>
        new(l.Id, l.Name, l.TypeId, l.Type.Value, l.TopicId, l.DisplayOrder, l.VideoUrl, l.HtmlContent, l.QuizId, l.Status);
}

/// <summary>A lesson quiz the Quiz type may point at; Inactive ones are offered only to the lesson that has one.</summary>
public sealed record LessonQuizChoiceDto(int Id, string Name, ActiveStatus Status);

/// <summary>Drop-downs of Lesson Details: lesson types (BR-16), the topics and the lesson quizzes of the subject.</summary>
public sealed record LessonChoicesDto(
    string Subject,
    IReadOnlyList<LookupDto> Types,
    IReadOnlyList<LookupDto> Topics,
    IReadOnlyList<LessonQuizChoiceDto> Quizzes);

public static class LessonRows
{
    public static LessonRowDto From(Lesson l, bool isTopic) =>
        new(l.Id, l.Name, l.DisplayOrder, l.Type.Value, isTopic, l.TopicId, l.Status);
}
