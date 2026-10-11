using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Application.Dtos.Public;

/// <summary>An active price package offered in Subject Register (UC-14 step 2).</summary>
public sealed record PackageOptionDto(int Id, string Name, int AccessMonths, decimal ListPrice, decimal SalePrice, string? Description)
{
    public static PackageOptionDto From(PricePackage p) => new(p.Id, p.Name, p.AccessMonths, p.ListPrice, p.SalePrice, p.Description);
}

/// <summary>The contact the registration copies from the profile; read-only in the pop-up (UC-14 step 4, OI-15).</summary>
public sealed record ContactDto(string FullName, string Email, string Mobile, Gender Gender);

/// <summary>UC-14 A3: the registration the account already has for this subject.</summary>
public sealed record ExistingRegistrationDto(int Id, string PackageName, decimal TotalCost, DateTime RegisteredAt);

/// <summary>
/// What the Subject Register pop-up needs. ContactProblems: profile fields that are empty
/// or in the wrong format, by field; Submit stays unavailable until they are fixed (E1).
/// Existing: set when A3 applies; the pop-up shows it instead of the form.
/// </summary>
public sealed record SubjectRegisterFormDto(
    int SubjectId,
    string SubjectName,
    IReadOnlyList<PackageOptionDto> Packages,
    ContactDto Contact,
    IReadOnlyDictionary<string, string[]> ContactProblems,
    ExistingRegistrationDto? Existing);

/// <summary>UC-14: the chosen package; everything else comes from the profile.</summary>
public sealed record SubjectRegisterRequest(int PackageId);

/// <summary>The stored registration (UC-14 step 7).</summary>
public sealed record SubjectRegisteredDto(int Id, string SubjectName, string PackageName, decimal TotalCost, RegistrationStatus Status);
