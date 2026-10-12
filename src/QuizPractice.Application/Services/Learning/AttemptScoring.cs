using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Application.Services.Learning;

/// <summary>
/// BR-21: an attempt passes when its score is at least the pass rate that applies — the
/// quiz's for a simulation exam, the subject's for a practice. An unanswered question
/// counts as wrong. The pass rate is stored with the result, so a later change to the
/// quiz or the subject does not change an old outcome.
/// </summary>
internal static class AttemptScoring
{
    public static int CorrectCount(Attempt attempt) =>
        attempt.Questions.Count(q => q.ChosenOptionId is { } chosen && q.Question.Options.Any(o => o.Id == chosen && o.IsCorrect));

    /// <summary>UC-29 steps 5 and 6: closes the attempt at that time and stores score and pass rate.</summary>
    public static void Score(Attempt attempt, DateTime finishedAt)
    {
        var count = attempt.Questions.Count;
        attempt.Score = count == 0 ? 0 : Math.Round(100m * CorrectCount(attempt) / count, 2);
        attempt.PassRate = attempt.Quiz?.PassRate ?? attempt.Subject.PassRate;
        attempt.FinishedAt = finishedAt;
        attempt.Status = AttemptStatus.Submitted;
    }

    /// <summary>
    /// Time the page needs to send its last answer and the submission once the countdown
    /// reaches zero; after it the server closes the exam by itself (UC-29 E1, UC-30 E1).
    /// </summary>
    public static readonly TimeSpan Grace = TimeSpan.FromSeconds(5);

    /// <summary>BR-06: a simulation exam ends its duration after it started; a practice has no end.</summary>
    public static DateTime? EndsAt(Attempt attempt) =>
        attempt is { Mode: AttemptMode.Exam, Quiz: { } quiz }
            ? DateTime.SpecifyKind(attempt.StartedAt, DateTimeKind.Utc).AddMinutes(quiz.DurationMin)
            : null;

    public static bool TimeIsUp(Attempt attempt, DateTime utcNow) =>
        attempt.Status == AttemptStatus.InProgress && EndsAt(attempt) is { } end && utcNow > end + Grace;

    /// <summary>
    /// UC-30 E1: an exam whose countdown ran out while the page was closed is scored as it
    /// stands, finished at its end time. True when the attempt was closed now.
    /// </summary>
    public static bool CloseIfTimeUp(Attempt attempt, DateTime utcNow)
    {
        if (!TimeIsUp(attempt, utcNow))
        {
            return false;
        }

        Score(attempt, EndsAt(attempt)!.Value);
        return true;
    }

    public static bool Passed(Attempt attempt) =>
        attempt is { Score: { } score, PassRate: { } passRate } && score >= passRate;
}
