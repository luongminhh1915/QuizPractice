using FluentValidation;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Common;
using QuizPractice.Application.Dtos.Public;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Application.Services.Public;

/// <summary>
/// What registering (UC-14) and changing a registration (UC-16) share: the packages on
/// offer, the contact copied from the profile and the check of that contact.
/// </summary>
internal static class RegistrationRules
{
    /// <summary>The active packages of the subject, the cheapest first.</summary>
    public static IEnumerable<PricePackage> ActivePackages(Subject s) =>
        s.PricePackages.Where(p => p.Status == ActiveStatus.Active).OrderBy(p => p.SalePrice).ThenBy(p => p.AccessMonths);

    public static ContactDto ContactOf(User u) => new(u.FullName, u.Email, u.Mobile, u.Gender);

    /// <summary>Checks the profile with the rules of User Register (BR-17), field by field (E1 of UC-14, E2 of UC-16).</summary>
    public static async Task<IReadOnlyDictionary<string, string[]>> ContactProblemsAsync(
        IValidator<RegisterRequest> validator, User customer, CancellationToken ct)
    {
        var result = await validator.ValidateAsync(
            new RegisterRequest(customer.FullName, customer.Gender, customer.Email, customer.Mobile), ct);
        return result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());
    }

    /// <summary>Copies the current profile into the registration, as every save does (UC-16 step 5).</summary>
    public static void CopyContact(Registration r, User customer)
    {
        r.FullName = customer.FullName;
        r.Email = customer.Email;
        r.Mobile = customer.Mobile;
        r.Gender = customer.Gender;
    }

    public static Error ProfileIncomplete(IReadOnlyDictionary<string, string[]> problems) =>
        new(ErrorType.Validation, "Profile.Incomplete",
            "Your profile is missing details the registration needs. Edit your profile first.", problems);

    public static Error PackageNotAvailable =>
        Error.Conflict("Package.NotAvailable", "This package is no longer offered. Choose another one.");
}
