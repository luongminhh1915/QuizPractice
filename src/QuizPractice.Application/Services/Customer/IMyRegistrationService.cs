using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Customer;
using QuizPractice.Application.Dtos.Public;
using QuizPractice.Domain.Common;

namespace QuizPractice.Application.Services.Customer;

/// <summary>UC-15 View My Registrations, UC-16 Edit, UC-17 Cancel: the customer's own registrations only.</summary>
public interface IMyRegistrationService
{
    Task<Result<Page<MyRegistrationDto>>> ListAsync(MyRegistrationListRequest request, CancellationToken ct = default);

    /// <summary>UC-16: the Subject Register pop-up again, on the stored package.</summary>
    Task<Result<SubjectRegisterFormDto>> GetChangeFormAsync(int id, CancellationToken ct = default);

    /// <summary>UC-16: another package, and the contact copied again from the profile; the status stays Submitted.</summary>
    Task<Result<MyRegistrationDto>> ChangeAsync(int id, ChangeRegistrationRequest request, CancellationToken ct = default);

    /// <summary>UC-17: the registration no longer waits for the Sale role; no access is granted.</summary>
    Task<Result<MyRegistrationDto>> CancelAsync(int id, CancellationToken ct = default);
}
