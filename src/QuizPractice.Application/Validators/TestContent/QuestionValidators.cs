using FluentValidation;
using QuizPractice.Application.Dtos.TestContent;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Constants;

namespace QuizPractice.Application.Validators.TestContent;

/// <summary>
/// UC-53 step 6 and E2 (BR-17): content filled, 2 to 6 options, exactly one marked
/// correct. Whether the subject, lesson, dimensions and level fit together needs the
/// database: QuestionService checks them.
/// </summary>
public class SaveQuestionRequestValidator : AbstractValidator<SaveQuestionRequest>
{
    public const int MinOptions = 2;
    public const int MaxOptions = 6;

    public SaveQuestionRequestValidator()
    {
        RuleFor(x => x.SubjectId).GreaterThan(0).WithMessage("Choose a subject.");
        RuleFor(x => x.LevelId).GreaterThan(0).WithMessage("Choose a level.");
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Content).NotEmpty().WithMessage("Enter the question.").MaximumLength(FieldLength.LongText);
        RuleFor(x => x.Explanation).MaximumLength(FieldLength.LongText);
        RuleFor(x => x.DimensionIds)
            .Must(ids => ids.Distinct().Count() == ids.Count).WithMessage("A dimension is listed twice.");

        RuleFor(x => x.Options)
            .Must(o => o.Count >= MinOptions).WithMessage($"Enter at least {MinOptions} answer options.")
            .Must(o => o.Count <= MaxOptions).WithMessage($"Enter at most {MaxOptions} answer options.")
            .Must(o => o.Count(x => x.IsCorrect) == 1).WithMessage("Mark exactly one option as the correct answer.")
            .Must(o => o.Where(x => x.Id is not null).Select(x => x.Id).Distinct().Count() == o.Count(x => x.Id is not null))
            .WithMessage("An option is listed twice.");
        RuleFor(x => x.Media!).SetValidator(new QuestionMediaValidator()).When(x => x.Media is not null);
        RuleForEach(x => x.Options).ChildRules(option =>
        {
            option.RuleFor(o => o.Content).NotEmpty().WithMessage("Enter the option text.")
                .MaximumLength(FieldLength.AnswerOption);
        });
    }
}

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
