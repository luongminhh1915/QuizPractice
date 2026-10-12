using FluentValidation;
using QuizPractice.Application.Dtos.CourseContent;
using QuizPractice.Application.Validators.Common;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Constants;

namespace QuizPractice.Application.Validators.CourseContent;

public class SubjectListRequestValidator : AbstractValidator<SubjectListRequest>
{
    private static readonly string[] SortColumns = ["id", "name", "category", "lessons", "owner", "status"];

    public SubjectListRequestValidator()
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

/// <summary>
/// UC-42 step 4 and E1 (BR-17). Whether the category is an active Subject Category and
/// the owner still holds the Expert role needs the database: SubjectService checks them (E2).
/// </summary>
public class CreateSubjectRequestValidator : AbstractValidator<CreateSubjectRequest>
{
    public CreateSubjectRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Enter the subject name.").MaximumLength(FieldLength.Name);
        RuleFor(x => x.CategoryId).GreaterThan(0).WithMessage("Choose a subject category.");
        RuleFor(x => x.ExpertId).GreaterThan(0).When(x => x.ExpertId is not null);
        RuleFor(x => x.PassRate).InclusiveBetween(0, 100).WithMessage("The pass rate is a percent from 0 to 100.");
        RuleFor(x => x.Description).MaximumLength(FieldLength.LongText);
        RuleFor(x => x.Thumbnail).NotNull().WithMessage("A thumbnail is required for a new subject.");
        RuleFor(x => x.Thumbnail!).SetValidator(new ImageUploadValidator()).When(x => x.Thumbnail is not null);
    }
}
