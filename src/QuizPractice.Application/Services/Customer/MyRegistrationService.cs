using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Customer;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.Application.Services.Customer;

/// <summary>
/// The registrations of the signed-in customer. They are found by account (user_id), not
/// by the email the step text names: a registration copies the email at the time, and
/// the account is what stays (UC-15 step 2).
/// </summary>
public class MyRegistrationService(
    IRegistrationRepository registrations,
    ICurrentUser currentUser) : IMyRegistrationService
{
    public async Task<Result<Page<MyRegistrationDto>>> ListAsync(MyRegistrationListRequest request, CancellationToken ct = default)
    {
        if (currentUser.Id is not { } userId)
        {
            return CustomerAccess.SignInRequired;
        }

        if (request.Page < 1 || request.PageSize is < 1 or > PageRequest.MaxPageSize)
        {
            return Error.Invalid(nameof(request.Page), "The page is out of range.");
        }

        // A1: no registration yet is an empty page; the screen offers the catalog.
        var page = await registrations.ListByUserAsync(userId, new PageRequest(request.Page, request.PageSize), ct);
        return page.Map(MyRegistrationDto.From);
    }
}
