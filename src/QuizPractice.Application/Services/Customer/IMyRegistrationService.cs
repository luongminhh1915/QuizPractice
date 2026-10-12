using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Customer;
using QuizPractice.Domain.Common;

namespace QuizPractice.Application.Services.Customer;

/// <summary>UC-15 View My Registrations, UC-16 Edit, UC-17 Cancel: the customer's own registrations only.</summary>
public interface IMyRegistrationService
{
    Task<Result<Page<MyRegistrationDto>>> ListAsync(MyRegistrationListRequest request, CancellationToken ct = default);
}
