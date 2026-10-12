using FluentValidation;
using QuizPractice.Application.Dtos.Sale;
using QuizPractice.Domain.Common;

namespace QuizPractice.Application.Validators.Sale;

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
