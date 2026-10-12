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

public static class LessonRows
{
    public static LessonRowDto From(Lesson l, bool isTopic) =>
        new(l.Id, l.Name, l.DisplayOrder, l.Type.Value, isTopic, l.TopicId, l.Status);
}
