using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.TestContent;
using QuizPractice.Domain.Common;

namespace QuizPractice.Application.Services.TestContent;

/// <summary>UC-56 View Quizzes List, UC-58 Delete Quiz.</summary>
public interface IQuizService
{
    Task<Result<Page<QuizSummaryDto>>> ListAsync(QuizListRequest request, CancellationToken ct = default);

    Task<Result<QuizFiltersDto>> GetFiltersAsync(CancellationToken ct = default);

    /// <summary>Removes a quiz nobody has taken and no lesson opens (BR-13).</summary>
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
}
