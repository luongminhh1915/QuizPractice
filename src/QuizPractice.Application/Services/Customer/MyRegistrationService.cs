using FluentValidation;
using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Common;
using QuizPractice.Application.Dtos.Customer;
using QuizPractice.Application.Dtos.Public;
using QuizPractice.Application.Services.Public;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.Application.Services.Customer;

/// <summary>
/// The registrations of the signed-in customer. They are found by account (user_id), not
/// by the email the step text names: a registration copies the email at the time, and
/// the account is what stays (UC-15 step 2).
/// </summary>
public class MyRegistrationService(
    IRegistrationRepository registrations,
    IUserRepository users,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IValidator<RegisterRequest> contactValidator) : IMyRegistrationService
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

    public async Task<Result<SubjectRegisterFormDto>> GetChangeFormAsync(int id, CancellationToken ct = default)
    {
        var (registration, customer, refusal) = await LoadChangeableAsync(id, ct);
        if (refusal is not null)
        {
            return refusal;
        }

        var subject = registration!.Subject;
        return new SubjectRegisterFormDto(
            subject.Id,
            subject.Name,
            RegistrationRules.ActivePackages(subject).Select(PackageOptionDto.From).ToList(),
            RegistrationRules.ContactOf(customer!),
            await RegistrationRules.ContactProblemsAsync(contactValidator, customer!, ct),
            Existing: null,
            SelectedPackageId: registration.PackageId);
    }

    public async Task<Result<MyRegistrationDto>> ChangeAsync(int id, ChangeRegistrationRequest request, CancellationToken ct = default)
    {
        var (registration, customer, refusal) = await LoadChangeableAsync(id, ct);
        if (refusal is not null)
        {
            return refusal;
        }

        // Step 4: a package of this subject that is still offered.
        var package = RegistrationRules.ActivePackages(registration!.Subject).FirstOrDefault(p => p.Id == request.PackageId);
        if (package is null)
        {
            return RegistrationRules.PackageNotAvailable;
        }

        // E2: as in UC-14 E1, the contact must be complete before anything is stored.
        var problems = await RegistrationRules.ContactProblemsAsync(contactValidator, customer!, ct);
        if (problems.Count > 0)
        {
            return RegistrationRules.ProfileIncomplete(problems);
        }

        // Step 5: new package and cost, the current profile, still Submitted.
        registration.PackageId = package.Id;
        registration.Package = package;
        registration.TotalCost = package.SalePrice;
        registration.UpdatedById = customer!.Id;
        RegistrationRules.CopyContact(registration, customer);

        // E1 at the last moment: the status is a concurrency token, so a Sale user who
        // settled it after it was read makes the save fail instead of being overwritten.
        if (!await unitOfWork.TrySaveChangesAsync(ct))
        {
            return Settled;
        }

        return MyRegistrationDto.From(registration);
    }

    /// <summary>
    /// The customer's own registration, still Submitted (E1) and in no unpaid invoice (E3,
    /// BR-24), with the customer; or the error that stops UC-16 and UC-17.
    /// </summary>
    private async Task<(Registration? Registration, User? Customer, Error? Refusal)> LoadChangeableAsync(int id, CancellationToken ct)
    {
        if (currentUser.Id is not { } userId)
        {
            return (null, null, CustomerAccess.SignInRequired);
        }

        var registration = await registrations.GetOwnAsync(id, userId, ct);
        var customer = await users.GetByIdAsync(userId, ct);
        if (registration is null || customer is null)
        {
            return (null, null, CustomerAccess.NotFound("Registration", id));
        }

        if (registration.Status != RegistrationStatus.Submitted)
        {
            return (null, null, Settled);
        }

        if (registration.Invoice is { Status: InvoiceStatus.Unpaid } invoice)
        {
            return (null, null, Error.Conflict("Registration.InUnpaidInvoice",
                $"This registration is waiting in invoice {invoice.Code}. Cancel that invoice first to change it."));
        }

        return (registration, customer, null);
    }

    private static Error Settled =>
        Error.Conflict("Registration.Settled", "This registration has been settled by the Sale team and can no longer be changed.");
}
