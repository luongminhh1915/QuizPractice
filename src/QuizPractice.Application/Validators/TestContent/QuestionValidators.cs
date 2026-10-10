using FluentValidation;
using QuizPractice.Application.Dtos.TestContent;
using QuizPractice.Domain.Common;

namespace QuizPractice.Application.Validators.TestContent;

/// <summary>Whether the user may reach the chosen subject needs the database: QuestionService checks it (UC-52 E1).</summary>
public class QuestionListRequestValidator : AbstractValidator<QuestionListRequest>
{
    private static readonly string[] SortColumns = ["id", "subject", "level", "status"];

    public QuestionListRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, PageRequest.MaxPageSize);
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.LessonId).Null()
            .When(x => x.SubjectId is null)
            .WithMessage("Choose a subject before a lesson.");
        RuleFor(x => x.DimensionIds).Empty()
            .When(x => x.SubjectId is null)
            .WithMessage("Choose a subject before a dimension.");
        RuleFor(x => x.SortBy!)
            .Must(s => SortColumns.Contains(s.ToLowerInvariant()))
            .WithMessage($"Sort by one of: {string.Join(", ", SortColumns)}.")
            .When(x => x.SortBy is not null);
    }
}
