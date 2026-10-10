using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.TestContent;
using QuizPractice.Domain.Common;

namespace QuizPractice.Application.Services.TestContent;

/// <summary>UC-56 View Quizzes List.</summary>
public interface IQuizService
{
    Task<Result<Page<QuizSummaryDto>>> ListAsync(QuizListRequest request, CancellationToken ct = default);

    Task<Result<QuizFiltersDto>> GetFiltersAsync(CancellationToken ct = default);
}
