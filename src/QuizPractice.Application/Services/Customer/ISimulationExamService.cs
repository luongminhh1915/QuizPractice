using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Customer;
using QuizPractice.Domain.Common;

namespace QuizPractice.Application.Services.Customer;

/// <summary>Simulation Exams (F#16; UC-22, UC-23): the exams of the subjects the customer may reach.</summary>
public interface ISimulationExamService
{
    /// <summary>UC-22: active simulation exams of the reachable subjects (BR-05, BR-23).</summary>
    Task<Result<Page<ExamSummaryDto>>> ListAsync(ExamListRequest request, CancellationToken ct = default);

    /// <summary>UC-22: the subjects to filter by; empty is E2.</summary>
    Task<Result<ExamFiltersDto>> GetFiltersAsync(CancellationToken ct = default);
}
