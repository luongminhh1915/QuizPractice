using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuizPractice.Domain.Constants;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Infrastructure.Persistence.Seed;

/// <summary>
/// Sample quizzes for development, written while the Quizzes table is empty, so a
/// database seeded before quizzes existed gets them too (no need to drop it). It finds
/// the sample subjects, chapters, dimensions and customer by name in the database and
/// skips with a warning when one is missing. Writes:
/// - Java OOP Practice Exam: simulation drawn by chapter, already taken once by the
///   sample customer (Taken label, UC-58 E2, UC-53 options already chosen);
/// - Chapter 1 Quiz: lesson quiz with a fixed set, used by lesson "1.3 Chapter 1 quiz" (UC-58 E3);
/// - Java OOP Mini Exam (retired): Inactive, nobody took it, nothing uses it, so it can be deleted;
/// - SQL Practice Exam: simulation drawn by Domain dimensions (SQL has no Expert: Admin only).
/// </summary>
internal static class DevQuizSeeder
{
    public const string JavaExam = "Java OOP Practice Exam";
    public const string ChapterQuiz = "Chapter 1 Quiz";
    public const string RetiredExam = "Java OOP Mini Exam (retired)";
    public const string SqlExam = "SQL Practice Exam";

    private const string JavaName = "Java Object-Oriented Programming";
    private const string SqlName = "SQL Fundamentals";
    private const string ByTopic = "Topic";
    private const string ByDomain = "Domain";

    public static async Task SeedAsync(AppDbContext db, ILogger logger, DateTime utcNow, CancellationToken ct = default)
    {
        if (await db.Quizzes.AnyAsync(ct))
        {
            return;
        }

        var java = await db.Subjects.FirstOrDefaultAsync(s => s.Name == JavaName, ct);
        var sql = await db.Subjects.FirstOrDefaultAsync(s => s.Name == SqlName, ct);
        var customer = await db.Users.FirstOrDefaultAsync(u => u.Email == DevSeedData.CustomerEmail, ct);
        var settings = await db.Settings.ToListAsync(ct);
        Setting? Find(SettingType type, string value) => settings.FirstOrDefault(s => s.Type == type && s.Value == value);

        var simulation = Find(SettingType.TestType, SettingValues.TestTypes.Simulation);
        var lessonQuiz = Find(SettingType.TestType, SettingValues.TestTypes.LessonQuiz);
        var quizLesson = Find(SettingType.LessonType, SettingValues.LessonTypes.Quiz);
        var easy = Find(SettingType.QuestionLevel, "Easy");
        var medium = Find(SettingType.QuestionLevel, "Medium");
        if (java is null || sql is null || customer is null || simulation is null || lessonQuiz is null
            || quizLesson is null || easy is null || medium is null)
        {
            logger.LogWarning("Sample quizzes not seeded: a sample subject, the customer account or a setting is missing.");
            return;
        }

        var lessons = await db.Lessons.Where(l => l.SubjectId == java.Id).ToListAsync(ct);
        var topics = lessons.Where(l => l.TopicId == null).OrderBy(l => l.DisplayOrder).ToList();
        var topicOf = lessons.Where(l => l.TopicId != null).ToDictionary(l => l.Id, l => l.TopicId!.Value);

        // Shown questions only: a quiz never draws a Hidden one (UC-54).
        var javaQuestions = await db.Questions.Include(q => q.Options)
            .Where(q => q.SubjectId == java.Id && q.Status == VisibilityStatus.Shown && q.LessonId != null)
            .OrderBy(q => q.Id).ToListAsync(ct);
        var byTopic = javaQuestions.Where(q => topicOf.ContainsKey(q.LessonId!.Value))
            .GroupBy(q => topicOf[q.LessonId!.Value]).ToDictionary(g => g.Key, g => g.ToList());
        if (topics.Count < 3 || topics.Take(3).Any(t => !byTopic.ContainsKey(t.Id)))
        {
            logger.LogWarning("Sample quizzes not seeded: {Subject} needs three chapters with shown questions.", JavaName);
            return;
        }

        var (ch1, ch2, ch3) = (topics[0], topics[1], topics[2]);

        // 1. Simulation exam by chapter, taken once.
        var exam = NewQuiz(java, simulation, medium, JavaExam, ByTopic, 20,
            "Covers the three chapters; a new set of questions is drawn for every attempt.");
        AddRule(exam, ch1.Id, Math.Min(3, byTopic[ch1.Id].Count));
        AddRule(exam, ch2.Id, Math.Min(3, byTopic[ch2.Id].Count));
        AddRule(exam, ch3.Id, Math.Min(2, byTopic[ch3.Id].Count));

        // 2. Lesson quiz with its fixed set, opened by a lesson of type Quiz under Chapter 1.
        var chapterQuiz = NewQuiz(java, lessonQuiz, easy, ChapterQuiz, ByTopic, 10,
            "Checks Chapter 1 at the end of the chapter; the same questions every time.");
        var fixedSet = byTopic[ch1.Id].Take(4).ToList();
        AddRule(chapterQuiz, ch1.Id, fixedSet.Count);
        for (var i = 0; i < fixedSet.Count; i++)
        {
            chapterQuiz.Questions.Add(new QuizQuestion { Quiz = chapterQuiz, QuestionId = fixedSet[i].Id, DisplayOrder = (short)(i + 1) });
        }

        db.Lessons.Add(new Lesson
        {
            SubjectId = java.Id,
            TopicId = ch1.Id,
            Type = quizLesson,
            Quiz = chapterQuiz,
            Name = "1.3 Chapter 1 quiz",
            DisplayOrder = lessons.Where(l => l.TopicId == ch1.Id).Select(l => l.DisplayOrder).DefaultIfEmpty().Max() + 1,
        });

        // 3. Inactive, unused: the one quiz that can be deleted.
        var retired = NewQuiz(java, simulation, easy, RetiredExam, ByTopic, 10,
            "An earlier short exam, no longer offered to customers.");
        retired.Status = ActiveStatus.Inactive;
        AddRule(retired, ch1.Id, Math.Min(2, byTopic[ch1.Id].Count));
        AddRule(retired, ch2.Id, Math.Min(2, byTopic[ch2.Id].Count));

        // 4. SQL exam by Domain dimensions.
        var sqlExam = NewQuiz(sql, simulation, medium, SqlExam, ByDomain, 15,
            "Two questions from each domain of the subject.");
        var domains = await db.SubjectDimensions
            .Where(d => d.SubjectId == sql.Id && d.Type.Value == ByDomain)
            .Select(d => new
            {
                d.Id,
                Shown = db.QuestionDimensions.Count(x => x.DimensionId == d.Id && x.Question.Status == VisibilityStatus.Shown),
            })
            .ToListAsync(ct);
        foreach (var domain in domains.Where(d => d.Shown > 0))
        {
            AddRule(sqlExam, domain.Id, Math.Min(2, domain.Shown));
        }

        var quizzes = new[] { exam, chapterQuiz, retired, sqlExam }.Where(q => q.Rules.Count > 0).ToList();
        db.Quizzes.AddRange(quizzes);
        db.Attempts.Add(TakenAttempt(exam, customer, byTopic, utcNow));
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Sample quizzes seeded: {Quizzes} quizzes and one submitted attempt of {Exam}.", quizzes.Count, JavaExam);
    }

    private static Quiz NewQuiz(Subject subject, Setting type, Setting level, string name, string selectionType,
        short durationMin, string description) => new()
    {
        Subject = subject,
        Type = type,
        Level = level,
        Name = name,
        Description = description,
        SelectionType = selectionType,
        DurationMin = durationMin,
        PassRate = subject.PassRate,
        Status = ActiveStatus.Active,
    };

    private static void AddRule(Quiz quiz, int groupId, int count)
    {
        if (count <= 0)
        {
            return;
        }

        quiz.Rules.Add(new QuizRule { Quiz = quiz, GroupId = groupId, QuestionCount = (short)count });
        quiz.QuestionCount += (short)count;
    }

    /// <summary>The sample customer's submitted attempt: every third answer wrong, scored as BR-21 says.</summary>
    private static Attempt TakenAttempt(Quiz exam, User customer, Dictionary<int, List<Question>> byTopic, DateTime utcNow)
    {
        var drawn = exam.Rules.SelectMany(r => byTopic[r.GroupId].Take(r.QuestionCount)).ToList();
        var startedAt = utcNow.Date.AddDays(-2).AddHours(9);
        var attempt = new Attempt
        {
            User = customer,
            SubjectId = exam.Subject.Id,
            Quiz = exam,
            Mode = AttemptMode.Exam,
            QuestionCount = (short)drawn.Count,
            CurrentOrder = (short)drawn.Count,
            StartedAt = startedAt,
            FinishedAt = startedAt.AddMinutes(14),
            PassRate = exam.PassRate,
            Status = AttemptStatus.Submitted,
        };

        var right = 0;
        for (var i = 0; i < drawn.Count; i++)
        {
            var options = drawn[i].Options.OrderBy(o => o.DisplayOrder).ToList();
            var wrong = (i + 1) % 3 == 0;
            var chosen = wrong ? options.First(o => !o.IsCorrect) : options.Single(o => o.IsCorrect);
            right += wrong ? 0 : 1;
            attempt.Questions.Add(new AttemptQuestion
            {
                Attempt = attempt,
                QuestionId = drawn[i].Id,
                ChosenOptionId = chosen.Id,
                DisplayOrder = (short)(i + 1),
            });
        }

        attempt.Score = Math.Round(100m * right / drawn.Count, 2);
        return attempt;
    }
}
