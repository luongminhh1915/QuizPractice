using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Learning;

namespace QuizPractice.Application.Services.Learning;

/// <summary>Quiz Review (F#18; UC-31 to UC-33): a scored attempt of the signed-in customer.</summary>
public interface IQuizReviewService
{
    /// <summary>UC-31: the stored answers next to the correct ones; 409 while the attempt still runs.</summary>
    Task<Result<QuizReviewDto>> GetAsync(int id, CancellationToken ct = default);
}
