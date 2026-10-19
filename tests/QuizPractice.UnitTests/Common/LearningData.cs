using QuizPractice.Domain.Constants;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.UnitTests.Common;

/// <summary>Builders for practices, simulation exams and their questions (UC-19 to UC-31).</summary>
public static class LearningData
{
    public static readonly Setting SimulationType = new() { Id = 31, Type = SettingType.TestType, Value = SettingValues.TestTypes.Simulation };
    public static readonly Setting LessonQuizType = new() { Id = 32, Type = SettingType.TestType, Value = "Lesson quiz" };
    public static readonly Setting Medium = new() { Id = 41, Type = SettingType.QuestionLevel, Value = "Medium" };

    /// <summary>A question with options A to D; option ids are id*10+1 .. id*10+4, the correct one at <paramref name="correct"/> (0-based).</summary>
    public static Question Question(int id, int correct = 0, string? media = null)
    {
        var question = new Question
        {
            Id = id,
            SubjectId = 1,
            Content = $"Question {id}?",
            Media = media,
            Explanation = $"Because of rule {id}.",
        };
        for (var i = 0; i < 4; i++)
        {
            question.Options.Add(new AnswerOption
            {
                Id = id * 10 + i + 1,
                QuestionId = id,
                Question = question,
                Content = $"Option {(char)('A' + i)}",
                IsCorrect = i == correct,
                DisplayOrder = (byte)(i + 1),
            });
        }

        return question;
    }

    public static int CorrectOptionOf(Question q) => q.Options.Single(o => o.IsCorrect).Id;

    public static int WrongOptionOf(Question q) => q.Options.First(o => !o.IsCorrect).Id;

    public static Quiz Exam(int id = 300, short durationMin = 30, byte passRate = 70, ActiveStatus status = ActiveStatus.Active,
        Setting? type = null, Subject? subject = null, params (int GroupId, short Count)[] rules)
    {
        subject ??= TestData.Subject();
        var quiz = new Quiz
        {
            Id = id,
            SubjectId = subject.Id,
            Subject = subject,
            TypeId = (type ?? SimulationType).Id,
            Type = type ?? SimulationType,
            LevelId = Medium.Id,
            Level = Medium,
            Name = "Java OOP Practice Exam",
            Description = "Thirty minutes, ten questions.",
            QuestionCount = (short)rules.Sum(r => r.Count),
            SelectionType = QuizSelection.Topic,
            DurationMin = durationMin,
            PassRate = passRate,
            Status = status,
        };
        quiz.Rules.AddRange(rules.Select(r => new QuizRule { QuizId = id, Quiz = quiz, GroupId = r.GroupId, QuestionCount = r.Count }));
        return quiz;
    }

    /// <summary>A running attempt of the customer over the given questions, in that order.</summary>
    public static Attempt Attempt(int id = 900, AttemptMode mode = AttemptMode.Practice, Quiz? quiz = null,
        DateTime? startedAt = null, int userId = TestData.CustomerId, params Question[] questions)
    {
        var subject = quiz?.Subject ?? TestData.Subject();
        var attempt = new Attempt
        {
            Id = id,
            UserId = userId,
            SubjectId = subject.Id,
            Subject = subject,
            QuizId = quiz?.Id,
            Quiz = quiz,
            Mode = mode,
            SelectionType = mode == AttemptMode.Practice ? SelectionType.Topic : null,
            QuestionCount = (short)questions.Length,
            StartedAt = startedAt ?? DateTime.UtcNow.AddMinutes(-5),
            Status = AttemptStatus.InProgress,
        };
        for (var i = 0; i < questions.Length; i++)
        {
            attempt.Questions.Add(new AttemptQuestion
            {
                AttemptId = id,
                Attempt = attempt,
                QuestionId = questions[i].Id,
                Question = questions[i],
                DisplayOrder = (short)(i + 1),
            });
        }

        return attempt;
    }

    public static Lesson Topic(int id, string name) => new() { Id = id, SubjectId = 1, Name = name };

    public static GroupMember[] Members(int groupId, params int[] questionIds) =>
        questionIds.Select(q => new GroupMember(groupId, q)).ToArray();

    /// <summary>The label lookups every attempt screen makes; empty unless a test needs names.</summary>
    public static void SetupLabels(Mock<IAttemptRepository> attempts)
    {
        attempts.Setup(a => a.GetLessonNamesAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, string>());
        attempts.Setup(a => a.GetDimensionNamesAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, string>());
        attempts.Setup(a => a.GetCorrectCountsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, int>());
        attempts.Setup(a => a.ListOpenExamsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
    }

    /// <summary>The subjects the customer may reach today (BR-05).</summary>
    public static void SetupAccess(Mock<IRegistrationRepository> registrations, params Subject[] subjects) =>
        registrations.Setup(r => r.ListAccessibleSubjectsAsync(It.IsAny<int>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(subjects);
}
