using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Customer;
using QuizPractice.Application.Services.Learning;
using QuizPractice.Domain.Constants;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.Application.Services.Customer;

/// <summary>
/// UC-71: a customer reaches a lesson quiz through the lessons of a subject they may reach
/// today (BR-05). The attempt asks the set the quiz drew at its last save, in its order,
/// so every customer meets the same questions (OI-07); unlike a simulation exam, nothing
/// is drawn here. The attempt is kept like an exam (D13): it runs against the quiz
/// duration, has no Peek, is scored with the quiz pass rate and resumed, not restarted.
/// </summary>
public class MySubjectService(
    IRegistrationRepository registrations,
    ILessonRepository lessons,
    IQuizRepository quizzes,
    IAttemptRepository attempts,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : IMySubjectService
{
    public async Task<Result<IReadOnlyList<MySubjectDto>>> ListAsync(CancellationToken ct = default)
    {
        if (currentUser.Id is not { } userId)
        {
            return CustomerAccess.SignInRequired;
        }

        var subjects = await AccessibleSubjectsAsync(userId, ct);
        return Result<IReadOnlyList<MySubjectDto>>.Success(
            subjects.Select(s => new MySubjectDto(s.Id, s.Name, s.Thumbnail, s.Brief)).ToList());
    }

    public async Task<Result<SubjectLessonsDto>> GetLessonsAsync(int subjectId, CancellationToken ct = default)
    {
        if (currentUser.Id is not { } userId)
        {
            return CustomerAccess.SignInRequired;
        }

        // E1: no Paid registration covers today, or the subject is not one of theirs.
        var subject = (await AccessibleSubjectsAsync(userId, ct)).FirstOrDefault(s => s.Id == subjectId);
        if (subject is null)
        {
            return CustomerAccess.NoAccess;
        }

        var rows = await lessons.ListForLearnerAsync(subjectId, ct);
        var open = await OpenAttemptsAsync(userId, ct);
        var topics = rows.Where(IsTopic).ToList();

        // A lesson under a topic that is turned off is out of sight with its topic.
        var groups = topics
            .Select(t => new LearnerTopicDto(t.Id, t.Name, Children(rows, t.Id, open)))
            .ToList();
        var loose = rows.Where(l => !IsTopic(l) && l.TopicId is null).Select(l => ToDto(l, open)).ToList();
        if (loose.Count > 0)
        {
            groups.Add(new LearnerTopicDto(null, null, loose));
        }

        return new SubjectLessonsDto(subject.Id, subject.Name, groups.Where(g => g.Lessons.Count > 0).ToList());
    }

    public async Task<Result<AttemptCreatedDto>> StartQuizAsync(int subjectId, int lessonId, CancellationToken ct = default)
    {
        if (currentUser.Id is not { } userId)
        {
            return CustomerAccess.SignInRequired;
        }

        // E1: the access may have ended since the lessons were loaded.
        if ((await AccessibleSubjectsAsync(userId, ct)).All(s => s.Id != subjectId))
        {
            return CustomerAccess.NoAccess;
        }

        var lesson = await lessons.GetByIdAsync(lessonId, ct);
        if (lesson is null || lesson.SubjectId != subjectId || lesson.Status != ActiveStatus.Active
            || lesson.Topic is { Status: not ActiveStatus.Active }
            || lesson.Type.Value != SettingValues.LessonTypes.Quiz || lesson.QuizId is not { } quizId)
        {
            return Error.NotFound("Lesson", lessonId);
        }

        // E2: the quiz was turned off, changed type or lost its set since the page was loaded.
        var quiz = await quizzes.GetWithRulesAsync(quizId, ct);
        if (quiz is null || !IsAvailable(quiz))
        {
            return Error.Conflict("LessonQuiz.NotAvailable",
                $"The quiz of \"{lesson.Name}\" is not available for now. Try again later or ask the expert of the subject.");
        }

        // A1: an unfinished attempt is resumed, never left for a second one.
        if ((await OpenAttemptsAsync(userId, ct)).ContainsKey(quiz.Id))
        {
            return Error.Conflict("LessonQuiz.Unfinished", "You have an unfinished attempt at this quiz. Resume it instead of starting a new one.");
        }

        var set = quiz.Questions.OrderBy(q => q.DisplayOrder).ToList();
        var attempt = new Attempt
        {
            UserId = userId,
            SubjectId = quiz.SubjectId,
            QuizId = quiz.Id,
            Mode = AttemptMode.Exam,
            QuestionCount = (short)set.Count,
            StartedAt = DateTime.UtcNow,
            Status = AttemptStatus.InProgress,
        };
        for (var i = 0; i < set.Count; i++)
        {
            attempt.Questions.Add(new AttemptQuestion { Attempt = attempt, QuestionId = set[i].QuestionId, DisplayOrder = (short)(i + 1) });
        }

        attempts.Add(attempt);
        await unitOfWork.SaveChangesAsync(ct);
        return new AttemptCreatedDto(attempt.Id);
    }

    private static bool IsTopic(Lesson l) => l.Type.Value == SettingValues.LessonTypes.SubjectTopic;

    private static bool IsAvailable(Quiz quiz) =>
        quiz.Status == ActiveStatus.Active
        && quiz.Type.Value == SettingValues.TestTypes.LessonQuiz
        && quiz.Questions.Count > 0;

    private static List<LearnerLessonDto> Children(IReadOnlyList<Lesson> rows, int topicId, IReadOnlyDictionary<int, int> open) =>
        rows.Where(l => l.TopicId == topicId && !IsTopic(l)).Select(l => ToDto(l, open)).ToList();

    private static LearnerLessonDto ToDto(Lesson l, IReadOnlyDictionary<int, int> open)
    {
        var quiz = l.Type.Value == SettingValues.LessonTypes.Quiz && l.Quiz is { } q
            ? new LessonQuizDto(q.Id, q.Name, q.Questions.Count, q.DurationMin, q.PassRate, IsAvailable(q), open.TryGetValue(q.Id, out var a) ? a : null)
            : null;
        return new LearnerLessonDto(l.Id, l.Name, l.Type.Value, quiz);
    }

    /// <summary>The customer's unfinished attempt at each quiz; one whose time ran out is scored first (as UC-30 E1).</summary>
    private async Task<IReadOnlyDictionary<int, int>> OpenAttemptsAsync(int userId, CancellationToken ct)
    {
        var open = await attempts.ListOpenExamsAsync(userId, ct);
        var now = DateTime.UtcNow;
        if (open.Count(a => AttemptScoring.CloseIfTimeUp(a, now)) > 0)
        {
            await unitOfWork.SaveChangesAsync(ct);
        }

        return open
            .Where(a => a.Status == AttemptStatus.InProgress && a.QuizId is not null)
            .GroupBy(a => a.QuizId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.StartedAt).First().Id);
    }

    private Task<IReadOnlyList<Subject>> AccessibleSubjectsAsync(int userId, CancellationToken ct) =>
        registrations.ListAccessibleSubjectsAsync(userId, VietnamTime.Today(DateTime.UtcNow), ct);
}
