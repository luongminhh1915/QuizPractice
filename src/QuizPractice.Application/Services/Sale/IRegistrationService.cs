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

    /// <summary>UC-60 step 4: the subjects on sale and their active packages.</summary>
    Task<Result<RegistrationChoicesDto>> GetChoicesAsync(CancellationToken ct = default);

    /// <summary>UC-60 step 2: one registration with what this Sale user may change in it.</summary>
    Task<Result<RegistrationDetailsDto>> GetAsync(int id, CancellationToken ct = default);

    /// <summary>UC-60: a registration recorded on behalf of a customer, Submitted.</summary>
    Task<Result<RegistrationDetailsDto>> CreateAsync(SaveRegistrationRequest request, CancellationToken ct = default);

    /// <summary>UC-60: the whole form, only for its recorder while Submitted (BR-22).</summary>
    Task<Result<RegistrationDetailsDto>> UpdateAsync(int id, SaveRegistrationRequest request, CancellationToken ct = default);

    /// <summary>
    /// UC-61: settles a Submitted registration. Paid sets the validity (BR-14) and, for an
    /// email without an account, creates one and mails its login information (BR-15).
    /// </summary>
    Task<Result<RegistrationStatusResultDto>> ChangeStatusAsync(int id, ChangeRegistrationStatusRequest request, CancellationToken ct = default);

    /// <summary>BR-22, UC-61 A3/A4: the notes alone, at any status.</summary>
    Task<Result<RegistrationDetailsDto>> SaveNotesAsync(int id, SaveNotesRequest request, CancellationToken ct = default);
}
