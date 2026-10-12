using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Learning;
using QuizPractice.Application.Services.Customer;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.Application.Services.Learning;

/// <summary>
/// Reads back what UC-29 stored. Open after the access to the subject has ended too: the
/// result belongs to the customer, and BR-05 limits starting attempts, not reading them.
/// </summary>
public class QuizReviewService(IAttemptRepository attempts, IUnitOfWork unitOfWork, ICurrentUser currentUser) : IQuizReviewService
{
    public async Task<Result<QuizReviewDto>> GetAsync(int id, CancellationToken ct = default)
    {
        if (currentUser.Id is not { } userId)
        {
            return CustomerAccess.SignInRequired;
        }

        var attempt = await attempts.GetOwnAsync(id, userId, ct);
        if (attempt is null)
        {
            return CustomerAccess.NotFound("Attempt", id);
        }

        // UC-30 E1: an exam that ran out of time with no page open is scored first.
        if (AttemptScoring.CloseIfTimeUp(attempt, DateTime.UtcNow))
        {
            await unitOfWork.SaveChangesAsync(ct);
        }

        // Pre-condition: scored by UC-29. Until then the correct answers stay on the server.
        if (attempt.Status != AttemptStatus.Submitted)
        {
            return Error.Conflict("Attempt.NotScored", "This attempt is still running. Submit it to review the answers.");
        }

        var label = (await AttemptLabels.LabelsAsync([attempt], attempts, ct))[attempt.Id];
        return new QuizReviewDto(
            attempt.Id,
            attempt.Subject.Name,
            label.Title,
            attempt.Mode,
            QuizHandleService.ResultOf(attempt),
            attempt.Questions.Select(ToDto).ToList());
    }

    private static ReviewQuestionDto ToDto(AttemptQuestion row)
    {
        var options = row.Question.Options;
        return new ReviewQuestionDto(
            row.DisplayOrder,
            row.QuestionId,
            row.Question.Content,
            QuestionMedia.TypeOf(row.Question.Media)?.Kind,
            options.Select((o, i) => new ReviewOptionDto(o.Id, QuizHandleService.Letter(i), o.Content, o.IsCorrect)).ToList(),
            row.ChosenOptionId,
            row.IsMarked,
            options.Any(o => o.Id == row.ChosenOptionId && o.IsCorrect),
            string.IsNullOrWhiteSpace(row.Question.Explanation) ? null : row.Question.Explanation);
    }
}
