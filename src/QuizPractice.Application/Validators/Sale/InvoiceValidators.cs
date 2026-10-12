using FluentValidation;
using QuizPractice.Application.Dtos.Sale;
using QuizPractice.Domain.Common;

namespace QuizPractice.Application.Validators.Sale;

public class InvoiceListRequestValidator : AbstractValidator<InvoiceListRequest>
{
    public InvoiceListRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, PageRequest.MaxPageSize);
        RuleFor(x => x.Search).MaximumLength(200);
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From)
            .When(x => x.From is not null && x.To is not null)
            .WithMessage("The date range ends before it starts.");
    }
}
