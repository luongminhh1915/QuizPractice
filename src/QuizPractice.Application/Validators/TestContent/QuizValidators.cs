using FluentValidation;
using QuizPractice.Application.Dtos.TestContent;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Constants;

namespace QuizPractice.Application.Validators.TestContent;

/// <summary>
/// UC-57 step 6, E3 and E4 (BR-17, BR-21; the CHECKs of SDS I.3). Whether the subject,
/// level, type and groups exist and hold enough questions needs the database:
/// QuizService checks them (E1, E2).
/// </summary>
public class SaveQuizRequestValidator : AbstractValidator<SaveQuizRequest>
{
    public const int MaxDuration = 180;
    public const int MaxQuestions = 100;

    public SaveQuizRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Enter the quiz name.").MaximumLength(FieldLength.Name);
        RuleFor(x => x.SubjectId).GreaterThan(0).WithMessage("Choose a subject.");
        RuleFor(x => x.LevelId).GreaterThan(0).WithMessage("Choose a level.");
        RuleFor(x => x.TypeId).GreaterThan(0).WithMessage("Choose the quiz type.");
        RuleFor(x => x.DurationMin).InclusiveBetween(1, MaxDuration)
            .WithMessage($"The duration is a whole number of minutes from 1 to {MaxDuration}.");
        RuleFor(x => x.PassRate).InclusiveBetween(0, 100).WithMessage("The pass rate is a percent from 0 to 100.");
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Description).MaximumLength(FieldLength.LongText);
        RuleFor(x => x.QuestionCount).InclusiveBetween(1, MaxQuestions)
            .WithMessage($"The number of questions is from 1 to {MaxQuestions}.");
        RuleFor(x => x.SelectionType).NotEmpty().WithMessage("Choose how the questions are drawn.");

        RuleFor(x => x.Rules)
            .NotEmpty().WithMessage("Add at least one group to draw the questions from.")
            .Must(r => r.Select(x => x.GroupId).Distinct().Count() == r.Count).WithMessage("A group is chosen twice.")
            .Must(r => r.All(x => x.QuestionCount >= 1)).WithMessage("Each group draws at least one question.")
            .Must((request, rules) => rules.Sum(x => x.QuestionCount) == request.QuestionCount)
            .WithMessage(request => $"The groups add up to {request.Rules.Sum(x => x.QuestionCount)} questions, not {request.QuestionCount}.")
            .When(x => x.Rules is not null);
    }
}

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
