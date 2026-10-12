using FluentValidation;
using QuizPractice.Application.Dtos.CourseContent;
using QuizPractice.Domain.Constants;

namespace QuizPractice.Application.Validators.CourseContent;

/// <summary>UC-47 step 4 and E1 (BR-14, BR-17; the CHECKs of SDS I.3): whole months 1 to 24, whole VND, sale not above list.</summary>
public class SavePricePackageRequestValidator : AbstractValidator<SavePricePackageRequest>
{
    public const int MaxMonths = 24;

    /// <summary>decimal(12,0) holds up to twelve digits.</summary>
    public const decimal MaxPrice = 999_999_999_999m;

    public SavePricePackageRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Enter the package name.").MaximumLength(FieldLength.Name);
        RuleFor(x => x.AccessMonths).InclusiveBetween(1, MaxMonths)
            .WithMessage($"The access duration is a whole number of months from 1 to {MaxMonths}.");
        RuleFor(x => x.ListPrice)
            .InclusiveBetween(0, MaxPrice).WithMessage("The list price is a whole number of VND, 0 or more.")
            .Must(p => p == decimal.Truncate(p)).WithMessage("The list price is a whole number of VND.");
        RuleFor(x => x.SalePrice)
            .InclusiveBetween(0, MaxPrice).WithMessage("The sale price is a whole number of VND, 0 or more.")
            .Must(p => p == decimal.Truncate(p)).WithMessage("The sale price is a whole number of VND.")
            .LessThanOrEqualTo(x => x.ListPrice).WithMessage("The sale price cannot be above the list price.");
        RuleFor(x => x.Description).MaximumLength(FieldLength.LongText);
    }
}
