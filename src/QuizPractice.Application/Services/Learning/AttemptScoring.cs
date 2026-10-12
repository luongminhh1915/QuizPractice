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

    public static bool Passed(Attempt attempt) =>
        attempt is { Score: { } score, PassRate: { } passRate } && score >= passRate;
}
