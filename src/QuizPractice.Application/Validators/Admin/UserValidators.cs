using FluentValidation;
using QuizPractice.Application.Dtos.Admin;
using QuizPractice.Application.Validators.Common;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Constants;

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

/// <summary>UC-63 step 4 (BR-17): the same rules as Register Account; the email being free needs the database.</summary>
public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.FullName).FullName();
        RuleFor(x => x.Gender).IsInEnum();
        RuleFor(x => x.Email).LoginEmail();
        RuleFor(x => x.Mobile).Mobile();
        RuleFor(x => x.Address).MaximumLength(FieldLength.Address);
        RuleFor(x => x.RoleId).GreaterThan(0).WithMessage("Choose the role.");
        RuleFor(x => x.Avatar!).SetValidator(new ImageUploadValidator()).When(x => x.Avatar is not null);
    }
}

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.RoleId).GreaterThan(0).WithMessage("Choose the role.");
        RuleFor(x => x.Status).IsInEnum();
    }
}
