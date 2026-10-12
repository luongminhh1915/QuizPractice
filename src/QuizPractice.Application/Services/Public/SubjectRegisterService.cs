using FluentValidation;
using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Common;
using QuizPractice.Application.Dtos.Public;
using QuizPractice.Domain.Constants;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.Application.Services.Public;

/// <summary>
/// UC-14. The contact is never typed into the pop-up: it is copied from the profile at
/// the moment of submitting, and checked with the same rules as User Register, so a
/// profile with a missing or malformed field blocks Submit until it is fixed (E1).
/// Pay by QR (A2) is left out until UC-18 exists (Duck 11/10).
/// </summary>
public class SubjectRegisterService(
    ISubjectRepository subjects,
    IRegistrationRepository registrations,
    IUserRepository users,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IValidator<RegisterRequest> contactValidator) : ISubjectRegisterService
{
    public async Task<Result<SubjectRegisterFormDto>> GetFormAsync(int subjectId, CancellationToken ct = default)
    {
        var (customer, subject, refusal) = await LoadAsync(subjectId, ct);
        if (refusal is not null)
        {
            return refusal;
        }

        var existing = await registrations.FindSubmittedAsync(customer!.Id, subjectId, ct);
        return new SubjectRegisterFormDto(
            subject!.Id,
            subject.Name,
            RegistrationRules.ActivePackages(subject).Select(PackageOptionDto.From).ToList(),
            RegistrationRules.ContactOf(customer),
            await RegistrationRules.ContactProblemsAsync(contactValidator, customer, ct),
            existing is null ? null : new ExistingRegistrationDto(
                existing.Id, existing.Package.Name, existing.TotalCost, DateTime.SpecifyKind(existing.RegisteredAt, DateTimeKind.Utc)));
    }

    public async Task<Result<SubjectRegisteredDto>> RegisterAsync(int subjectId, SubjectRegisterRequest request, CancellationToken ct = default)
    {
        var (customer, subject, refusal) = await LoadAsync(subjectId, ct);
        if (refusal is not null)
        {
            return refusal;
        }

        // A3: one Submitted registration per subject; the pop-up shows the existing one.
        if (await registrations.FindSubmittedAsync(customer!.Id, subjectId, ct) is not null)
        {
            return Error.Conflict("Registration.AlreadySubmitted",
                "You already have a Submitted registration for this subject. It can be changed until it is settled.");
        }

        // E2: deactivated (or never of this subject) since the pop-up was opened.
        var package = RegistrationRules.ActivePackages(subject!).FirstOrDefault(p => p.Id == request.PackageId);
        if (package is null)
        {
            return RegistrationRules.PackageNotAvailable;
        }

        // E1: the contact must be complete and valid before anything is stored.
        var problems = await RegistrationRules.ContactProblemsAsync(contactValidator, customer, ct);
        if (problems.Count > 0)
        {
            return RegistrationRules.ProfileIncomplete(problems);
        }

        var registration = new Registration
        {
            SubjectId = subject!.Id,
            PackageId = package.Id,
            UserId = customer.Id,
            CreatedById = null,
            UpdatedById = customer.Id,
            TotalCost = package.SalePrice,
            Status = RegistrationStatus.Submitted,
            RegisteredAt = DateTime.UtcNow,
        };
        RegistrationRules.CopyContact(registration, customer);
        registrations.Add(registration);
        await unitOfWork.SaveChangesAsync(ct);
        return new SubjectRegisteredDto(registration.Id, subject.Name, package.Name, registration.TotalCost, registration.Status);
    }

    /// <summary>The signed-in customer and the published subject, or the error that stops the use case.</summary>
    private async Task<(User? Customer, Subject? Subject, Error? Refusal)> LoadAsync(int subjectId, CancellationToken ct)
    {
        if (currentUser.Id is not { } userId)
        {
            return (null, null, new Error(ErrorType.Unauthorized, "Auth.Required", "Log in as a customer to register for a subject."));
        }

        // OI-15: only a customer registers; the other roles never see the action.
        if (!currentUser.IsInRole(Roles.Customer))
        {
            return (null, null, new Error(ErrorType.Forbidden, "Registration.CustomerOnly", "Only a customer can register for a subject."));
        }

        var customer = await users.GetByIdAsync(userId, ct);
        var subject = await subjects.GetPublishedAsync(subjectId, ct);
        if (customer is null || subject is null)
        {
            return (null, null, new Error(ErrorType.NotFound, "Subject.NotAvailable", "This subject is not available."));
        }

        return (customer, subject, null);
    }

}
