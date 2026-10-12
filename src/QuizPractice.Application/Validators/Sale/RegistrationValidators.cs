using FluentValidation;
using QuizPractice.Application.Dtos.Sale;
using QuizPractice.Application.Validators.Common;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Constants;

namespace QuizPractice.Application.Validators.Sale;

/// <summary>
/// UC-60 step 6, E1 (BR-17): the contact rules of User Register. Whether the subject and
/// the package are still on sale needs the database: RegistrationService checks it (E2).
/// </summary>
public class SaveRegistrationRequestValidator : AbstractValidator<SaveRegistrationRequest>
{
    public SaveRegistrationRequestValidator()
    {
        RuleFor(x => x.SubjectId).GreaterThan(0).WithMessage("Choose a subject.");
        RuleFor(x => x.PackageId).GreaterThan(0).WithMessage("Choose a package.");
        RuleFor(x => x.FullName).FullName();
        RuleFor(x => x.Gender).NotNull().WithMessage("Choose the gender.").IsInEnum();
        RuleFor(x => x.Email).LoginEmail();
        RuleFor(x => x.Mobile).Mobile();
        RuleFor(x => x.Notes).MaximumLength(FieldLength.Brief);
    }
}

public class ChangeRegistrationStatusRequestValidator : AbstractValidator<ChangeRegistrationStatusRequest>
{
    public ChangeRegistrationStatusRequestValidator()
    {
        // BR-22: a Submitted registration moves to Paid or to Cancelled, nowhere else.
        RuleFor(x => x.Status)
            .Must(s => s is Domain.Enums.RegistrationStatus.Paid or Domain.Enums.RegistrationStatus.Cancelled)
            .WithMessage("Set the status to Paid or Cancelled.");
        RuleFor(x => x.Notes).MaximumLength(FieldLength.Brief);
    }
}

public class SaveNotesRequestValidator : AbstractValidator<SaveNotesRequest>
{
    public SaveNotesRequestValidator() => RuleFor(x => x.Notes).MaximumLength(FieldLength.Brief);
}

public class RegistrationListRequestValidator : AbstractValidator<RegistrationListRequest>
{
    private static readonly string[] SortColumns =
        ["id", "email", "registeredat", "subject", "package", "totalcost", "status", "validfrom", "validto"];

    public RegistrationListRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, PageRequest.MaxPageSize);
        RuleFor(x => x.Search).MaximumLength(200);
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.SortBy!)
            .Must(s => SortColumns.Contains(s.ToLowerInvariant()))
            .When(x => x.SortBy is not null)
            .WithMessage("Sort by id, email, registration time, subject, package, total cost, status, valid from or valid to.");

        // UC-59 E1: the range is refused and the page keeps the previous one.
        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From)
            .When(x => x.From is not null && x.To is not null)
            .WithMessage("The registration-time range ends before it starts.");
    }
}
