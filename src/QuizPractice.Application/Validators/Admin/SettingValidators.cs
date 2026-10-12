using FluentValidation;
using QuizPractice.Application.Dtos.Admin;
using QuizPractice.Domain.Common;

namespace QuizPractice.Application.Validators.Admin;

public class SettingListRequestValidator : AbstractValidator<SettingListRequest>
{
    private static readonly string[] SortColumns = ["id", "type", "value", "order", "status"];

    public SettingListRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, PageRequest.MaxPageSize);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.SortBy!)
            .Must(s => SortColumns.Contains(s.ToLowerInvariant()))
            .WithMessage($"Sort by one of: {string.Join(", ", SortColumns)}.")
            .When(x => x.SortBy is not null);
    }
}
