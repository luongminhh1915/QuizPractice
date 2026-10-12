using FluentValidation;
using QuizPractice.Application.Dtos.Customer;
using QuizPractice.Domain.Common;

namespace QuizPractice.Application.Validators.Customer;

public class ExamListRequestValidator : AbstractValidator<ExamListRequest>
{
    private static readonly string[] SortColumns = ["id", "subject", "name", "level"];

    public ExamListRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, PageRequest.MaxPageSize);
        RuleFor(x => x.Search).MaximumLength(200);
        RuleFor(x => x.SortBy!)
            .Must(s => SortColumns.Contains(s.ToLowerInvariant()))
            .When(x => x.SortBy is not null)
            .WithMessage("Sort by id, subject, name or level.");
    }
}
