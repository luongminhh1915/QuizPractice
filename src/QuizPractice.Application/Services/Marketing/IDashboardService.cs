using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Marketing;

namespace QuizPractice.Application.Services.Marketing;

/// <summary>UC-34 View Management Dashboard.</summary>
public interface IDashboardService
{
    Task<Result<DashboardDto>> GetAsync(DashboardRequest request, CancellationToken ct = default);
}
