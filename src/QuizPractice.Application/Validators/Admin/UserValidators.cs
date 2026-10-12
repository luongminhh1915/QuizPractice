using FluentValidation;
using QuizPractice.Application.Dtos.Admin;
using QuizPractice.Domain.Common;

namespace QuizPractice.Application.Validators.Admin;

public class UserListRequestValidator : AbstractValidator<UserListRequest>
{
    private static readonly string[] SortColumns = ["id", "fullname", "gender", "email", "mobile", "role", "status"];

    public UserListRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, PageRequest.MaxPageSize);
        RuleFor(x => x.Gender).IsInEnum();
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.SortBy!)
            .Must(s => SortColumns.Contains(s.ToLowerInvariant()))
            .WithMessage($"Sort by one of: {string.Join(", ", SortColumns)}.")
            .When(x => x.SortBy is not null);
    }
}
