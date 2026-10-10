using FluentValidation;
using QuizPractice.Application.Dtos.Marketing;
using QuizPractice.Domain.Common;

namespace QuizPractice.Application.Validators.Marketing;

public class SliderListRequestValidator : AbstractValidator<SliderListRequest>
{
    private static readonly string[] SortColumns = ["id", "title", "backlink", "status"];

    public SliderListRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, PageRequest.MaxPageSize);
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.SortBy!)
            .Must(s => SortColumns.Contains(s.ToLowerInvariant()))
            .WithMessage($"Sort by one of: {string.Join(", ", SortColumns)}.")
            .When(x => x.SortBy is not null);
    }
}
