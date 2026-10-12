using FluentValidation;
using QuizPractice.Application.Dtos.CourseContent;
using QuizPractice.Domain.Constants;

namespace QuizPractice.Application.Validators.CourseContent;

/// <summary>UC-46 step 4 and E2; whether the type is an active Dimension Type is checked by SubjectDimensionService.</summary>
public class SaveDimensionRequestValidator : AbstractValidator<SaveDimensionRequest>
{
    public SaveDimensionRequestValidator()
    {
        RuleFor(x => x.TypeId).GreaterThan(0).WithMessage("Choose the dimension type.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Enter the dimension name.").MaximumLength(FieldLength.Name);
        RuleFor(x => x.Description).MaximumLength(FieldLength.LongText);
    }
}
