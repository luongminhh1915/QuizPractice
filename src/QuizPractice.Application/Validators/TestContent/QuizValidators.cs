using FluentValidation;
using QuizPractice.Application.Dtos.TestContent;
using QuizPractice.Domain.Common;

namespace QuizPractice.Application.Validators.TestContent;

/// <summary>Whether the user may reach the chosen subject needs the database: QuizService checks it (UC-56 E1).</summary>
public class QuizListRequestValidator : AbstractValidator<QuizListRequest>
{
    private static readonly string[] SortColumns = ["id", "subject", "level"];

    public QuizListRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, PageRequest.MaxPageSize);
        RuleFor(x => x.SortBy!)
            .Must(s => SortColumns.Contains(s.ToLowerInvariant()))
            .WithMessage($"Sort by one of: {string.Join(", ", SortColumns)}.")
            .When(x => x.SortBy is not null);
    }
}
