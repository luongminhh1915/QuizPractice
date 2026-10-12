using FluentValidation;
using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.CourseContent;
using QuizPractice.Application.Validators;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Constants;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.Application.Services.CourseContent;

public class LessonService(
    ILessonRepository lessons,
    ISubjectRepository subjects,
    ICurrentUser currentUser,
    IValidator<LessonListRequest> listValidator) : ILessonService
{
    public async Task<Result<LessonListDto>> ListAsync(int subjectId, LessonListRequest request, CancellationToken ct = default)
    {
        var subject = await CheckSubjectAsync(subjectId, ct);
        if (subject.Error is { } denied)
        {
            return denied;
        }

        if (await listValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        var stored = await lessons.ListAsync(subjectId, ct);
        var rows = InTopicOrder(stored);

        // Filters of SRS III 6.6: a topic keeps its own row and the lessons under it.
        IEnumerable<(Lesson Lesson, bool IsTopic)> shown = rows;
        if (request.TopicId is { } topicId)
        {
            shown = shown.Where(r => r.Lesson.Id == topicId || r.Lesson.TopicId == topicId);
        }

        if (request.Status is { } status)
        {
            shown = shown.Where(r => r.Lesson.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var text = request.Search.Trim();
            shown = shown.Where(r => r.Lesson.Name.Contains(text, StringComparison.OrdinalIgnoreCase));
        }

        // A subject holds tens of lessons, not thousands: ordering under the topics is
        // simpler here than in SQL, and the page is cut from the ordered rows.
        var matching = shown.ToList();
        var page = new PageRequest(request.Page, request.PageSize);
        var items = matching.Skip(page.Skip).Take(page.PageSize).Select(r => LessonRows.From(r.Lesson, r.IsTopic)).ToList();
        var topics = rows.Where(r => r.IsTopic).Select(r => new LookupDto(r.Lesson.Id, r.Lesson.Name)).ToList();
        return new LessonListDto(subjectId, subject.Value.Name, topics,
            new Page<LessonRowDto>(items, matching.Count, page.PageNumber, page.PageSize));
    }

    /// <summary>BR-11 for the subject the lessons belong to: E1 for another Expert's subject.</summary>
    private async Task<Result<Subject>> CheckSubjectAsync(int subjectId, CancellationToken ct)
    {
        if (CourseAccess.Check(currentUser, out var expertId) is { } denied)
        {
            return denied;
        }

        var subject = await subjects.GetByIdAsync(subjectId, ct);
        if (subject is null)
        {
            return Error.NotFound("Subject", subjectId);
        }

        return CourseAccess.CheckSubject(subject, expertId) is { } notAssigned ? notAssigned : subject;
    }

    private static bool IsTopic(Lesson lesson) => lesson.Type.Value == SettingValues.LessonTypes.SubjectTopic;

    /// <summary>
    /// Each topic in its order, followed by the lessons and quizzes under it in theirs; a
    /// lesson whose topic is missing comes last.
    /// </summary>
    private static List<(Lesson Lesson, bool IsTopic)> InTopicOrder(IReadOnlyList<Lesson> all)
    {
        var topics = all.Where(IsTopic).ToList();
        var children = all.Where(l => !IsTopic(l)).ToLookup(l => l.TopicId);
        var rows = new List<(Lesson, bool)>();
        foreach (var topic in topics)
        {
            rows.Add((topic, true));
            rows.AddRange(children[topic.Id].Select(l => (l, false)));
        }

        var placed = topics.Select(t => (int?)t.Id).ToHashSet();
        rows.AddRange(all.Where(l => !IsTopic(l) && !placed.Contains(l.TopicId)).Select(l => (l, false)));
        return rows;
    }
}
