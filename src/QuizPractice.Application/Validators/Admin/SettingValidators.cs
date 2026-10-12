using FluentValidation;
using QuizPractice.Application.Dtos.Admin;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Constants;

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

/// <summary>UC-65 step 4 (BR-17); the value being free in its type needs the database.</summary>
public class CreateSettingRequestValidator : AbstractValidator<CreateSettingRequest>
{
    public CreateSettingRequestValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Value).NotEmpty().WithMessage("Enter the value.").MaximumLength(FieldLength.Name);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(1).WithMessage("The order is a whole number from 1.").When(x => x.DisplayOrder is not null);
        RuleFor(x => x.Description).MaximumLength(FieldLength.Brief);
        RuleFor(x => x.Status).IsInEnum();
    }
}

public class UpdateSettingRequestValidator : AbstractValidator<UpdateSettingRequest>
{
    public UpdateSettingRequestValidator()
    {
        RuleFor(x => x.Value).NotEmpty().WithMessage("Enter the value.").MaximumLength(FieldLength.Name);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(1).WithMessage("The order is a whole number from 1.").When(x => x.DisplayOrder is not null);
        RuleFor(x => x.Description).MaximumLength(FieldLength.Brief);
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Version).NotEmpty().WithMessage("The version the form was loaded with is missing.");
    }
}
