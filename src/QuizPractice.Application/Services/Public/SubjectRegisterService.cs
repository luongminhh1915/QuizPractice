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
            ActivePackages(subject).Select(PackageOptionDto.From).ToList(),
            new ContactDto(customer.FullName, customer.Email, customer.Mobile, customer.Gender),
            await ContactProblemsAsync(customer, ct),
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
        var package = ActivePackages(subject!).FirstOrDefault(p => p.Id == request.PackageId);
        if (package is null)
        {
            return Error.Conflict("Package.NotAvailable", "This package is no longer offered. Choose another one.");
        }

        // E1: the contact must be complete and valid before anything is stored.
        var problems = await ContactProblemsAsync(customer, ct);
        if (problems.Count > 0)
        {
            return new Error(ErrorType.Validation, "Profile.Incomplete",
                "Your profile is missing details the registration needs. Edit your profile first.", problems);
        }

        var registration = new Registration
        {
            SubjectId = subject!.Id,
            PackageId = package.Id,
            UserId = customer.Id,
            CreatedById = null,
            UpdatedById = customer.Id,
            FullName = customer.FullName,
            Email = customer.Email,
            Mobile = customer.Mobile,
            Gender = customer.Gender,
            TotalCost = package.SalePrice,
            Status = RegistrationStatus.Submitted,
            RegisteredAt = DateTime.UtcNow,
        };
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

    private static IEnumerable<PricePackage> ActivePackages(Subject s) =>
        s.PricePackages.Where(p => p.Status == ActiveStatus.Active).OrderBy(p => p.SalePrice).ThenBy(p => p.AccessMonths);

    /// <summary>Checks the profile with the rules of User Register (BR-17), field by field.</summary>
    private async Task<IReadOnlyDictionary<string, string[]>> ContactProblemsAsync(User customer, CancellationToken ct)
    {
        var result = await contactValidator.ValidateAsync(
            new RegisterRequest(customer.FullName, customer.Gender, customer.Email, customer.Mobile), ct);
        return result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());
    }
}
