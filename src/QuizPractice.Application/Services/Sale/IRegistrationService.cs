using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Sale;
using QuizPractice.Domain.Common;

namespace QuizPractice.Application.Services.Sale;

/// <summary>Registrations List and Registration Details (F#35, F#36; UC-59 to UC-61), for the Sale role.</summary>
public interface IRegistrationService
{
    /// <summary>UC-59: every registration, the newest first unless another order is asked.</summary>
    Task<Result<Page<RegistrationSummaryDto>>> ListAsync(RegistrationListRequest request, CancellationToken ct = default);

    /// <summary>UC-59: the subjects to filter by.</summary>
    Task<Result<RegistrationFiltersDto>> GetFiltersAsync(CancellationToken ct = default);
}
