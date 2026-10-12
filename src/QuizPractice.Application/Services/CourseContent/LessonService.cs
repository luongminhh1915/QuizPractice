using FluentValidation;
using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.CourseContent;
using QuizPractice.Application.Validators;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Constants;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.Application.Services.CourseContent;

public class LessonService(
    ILessonRepository lessons,
    ISubjectRepository subjects,
    ISettingRepository settings,
    IUnitOfWork unitOfWork,
    IHtmlSanitizer sanitizer,
    ICurrentUser currentUser,
    IValidator<LessonListRequest> listValidator,
    IValidator<SaveLessonRequest> saveValidator) : ILessonService
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

    public async Task<Result<LessonChoicesDto>> GetChoicesAsync(int subjectId, CancellationToken ct = default)
    {
        var subject = await CheckSubjectAsync(subjectId, ct);
        if (subject.Error is { } denied)
        {
            return denied;
        }

        var types = await settings.ListActiveAsync(SettingType.LessonType, ct);
        var topics = (await lessons.ListAsync(subjectId, ct)).Where(IsTopic);
        var quizzes = await lessons.ListLessonQuizzesAsync(subjectId, ct);
        return new LessonChoicesDto(
            subject.Value.Name,
            types.Select(t => new LookupDto(t.Id, t.Value)).ToList(),
            topics.Select(t => new LookupDto(t.Id, t.Name)).ToList(),
            quizzes.Select(q => new LessonQuizChoiceDto(q.Id, q.Name, q.Status)).ToList());
    }

    public async Task<Result<LessonDto>> GetAsync(int subjectId, int lessonId, CancellationToken ct = default)
    {
        if ((await CheckSubjectAsync(subjectId, ct)).Error is { } denied)
        {
            return denied;
        }

        var lesson = await lessons.GetByIdAsync(lessonId, ct);
        return lesson is null || lesson.SubjectId != subjectId ? Error.NotFound("Lesson", lessonId) : LessonDto.From(lesson);
    }

    public async Task<Result<LessonDto>> CreateAsync(int subjectId, SaveLessonRequest request, CancellationToken ct = default)
    {
        if ((await CheckSubjectAsync(subjectId, ct)).Error is { } denied)
        {
            return denied;
        }

        var lesson = new Lesson { SubjectId = subjectId, Status = ActiveStatus.Active };
        if (await ApplyAsync(lesson, request, ct) is { } invalid)
        {
            return invalid;
        }

        lessons.Add(lesson);
        await unitOfWork.SaveChangesAsync(ct);
        return LessonDto.From(lesson);
    }

    public async Task<Result<LessonDto>> UpdateAsync(int subjectId, int lessonId, SaveLessonRequest request, CancellationToken ct = default)
    {
        if ((await CheckSubjectAsync(subjectId, ct)).Error is { } denied)
        {
            return denied;
        }

        var lesson = await lessons.GetByIdAsync(lessonId, ct);
        if (lesson is null || lesson.SubjectId != subjectId)
        {
            return Error.NotFound("Lesson", lessonId);
        }

        if (await ApplyAsync(lesson, request, ct) is { } invalid)
        {
            return invalid;
        }

        await unitOfWork.SaveChangesAsync(ct);
        return LessonDto.From(lesson);
    }

    /// <summary>
    /// UC-50 steps 3 to 6: the type decides which fields are kept (F#29), and a field the
    /// type does not have is cleared, so a lesson never keeps a video it no longer shows (A2).
    /// </summary>
    private async Task<Error?> ApplyAsync(Lesson lesson, SaveLessonRequest request, CancellationToken ct)
    {
        if (await saveValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        var type = await settings.GetByIdAsync(request.TypeId, ct);
        var isNew = lesson.Id == 0;
        if (type is null || type.Type != SettingType.LessonType
            || (type.Status == ActiveStatus.Inactive && type.Id != lesson.TypeId)
            || type.Value is not (SettingValues.LessonTypes.SubjectTopic or SettingValues.LessonTypes.Lesson or SettingValues.LessonTypes.Quiz))
        {
            return Error.Invalid(nameof(SaveLessonRequest.TypeId), "Choose Subject Topic, Lesson or Quiz.");
        }

        var becomesTopic = type.Value == SettingValues.LessonTypes.SubjectTopic;

        // A topic that holds lessons would leave them under nothing.
        if (!isNew && !becomesTopic && IsTopic(lesson) && await lessons.HasLessonsUnderAsync(lesson.Id, ct))
        {
            return Error.Conflict("Lesson.TopicHasLessons",
                $"Lessons sit under \"{lesson.Name}\". Move them to another topic before it becomes a {type.Value}.");
        }

        int? topicId = null;
        if (!becomesTopic)
        {
            // Step 5: a Lesson or a Quiz sits under a Subject Topic of the same subject.
            var topic = request.TopicId is { } id ? await lessons.GetByIdAsync(id, ct) : null;
            if (topic is null || topic.SubjectId != lesson.SubjectId || !IsTopic(topic) || topic.Id == lesson.Id)
            {
                return Error.Invalid(nameof(SaveLessonRequest.TopicId), "Choose the Subject Topic this lesson sits under.");
            }

            topicId = topic.Id;
        }

        int? quizId = null;
        if (type.Value == SettingValues.LessonTypes.Quiz)
        {
            // A3 and E3: a lesson quiz of the same subject; an Inactive one only if the lesson already had it.
            var quizzes = await lessons.ListLessonQuizzesAsync(lesson.SubjectId, ct);
            if (quizzes.Count == 0)
            {
                return Error.Invalid(nameof(SaveLessonRequest.QuizId),
                    "The subject has no lesson quiz yet. Prepare one in Quizzes List first.");
            }

            var quiz = quizzes.FirstOrDefault(q => q.Id == request.QuizId);
            if (quiz is null || (quiz.Status == ActiveStatus.Inactive && quiz.Id != lesson.QuizId))
            {
                return Error.Invalid(nameof(SaveLessonRequest.QuizId), "Choose an active lesson quiz of this subject.");
            }

            quizId = quiz.Id;
        }

        string? content = null;
        if (type.Value == SettingValues.LessonTypes.Lesson)
        {
            content = sanitizer.Sanitize(request.HtmlContent ?? string.Empty);
            if (content.Length > FieldLength.LongText)
            {
                return Error.Invalid(nameof(SaveLessonRequest.HtmlContent),
                    $"The content must be {FieldLength.LongText:N0} characters or fewer.");
            }
        }

        var moved = isNew || topicId != lesson.TopicId || becomesTopic != IsTopic(lesson);
        lesson.Name = request.Name.Trim();
        lesson.TypeId = type.Id;
        lesson.Type = type;
        lesson.TopicId = topicId;
        lesson.QuizId = quizId;
        lesson.VideoUrl = type.Value == SettingValues.LessonTypes.Lesson && !string.IsNullOrWhiteSpace(request.VideoUrl)
            ? request.VideoUrl.Trim()
            : null;
        lesson.HtmlContent = string.IsNullOrEmpty(content) ? null : content;
        lesson.DisplayOrder = request.DisplayOrder
            ?? (moved ? await lessons.NextOrderAsync(lesson.SubjectId, topicId, ct) : lesson.DisplayOrder);
        return null;
    }

    public async Task<Result<LessonRowDto>> ChangeStatusAsync(
        int subjectId, int lessonId, ChangeLessonStatusRequest request, CancellationToken ct = default)
    {
        if ((await CheckSubjectAsync(subjectId, ct)).Error is { } denied)
        {
            return denied;
        }

        if (!Enum.IsDefined(request.Status))
        {
            return Error.Invalid(nameof(request.Status), "Status must be Active or Inactive.");
        }

        var lesson = await lessons.GetByIdAsync(lessonId, ct);
        if (lesson is null || lesson.SubjectId != subjectId)
        {
            return Error.NotFound("Lesson", lessonId);
        }

        // A1: already in the chosen state, so nothing is written.
        if (lesson.Status != request.Status)
        {
            lesson.Status = request.Status;
            await unitOfWork.SaveChangesAsync(ct);
        }

        return LessonRows.From(lesson, IsTopic(lesson));
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
