using FluentValidation;
using QuizPractice.Application.Dtos.Common;
using QuizPractice.Domain.Constants;

namespace QuizPractice.Application.Validators.Common;

/// <summary>UC-01 step 3: both fields filled. Nothing else is told before the credentials are checked.</summary>
public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Enter the email.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Enter the password.");
    }
}

public class SendVerificationLinkRequestValidator : AbstractValidator<SendVerificationLinkRequest>
{
    public SendVerificationLinkRequestValidator()
    {
        RuleFor(x => x.Email).LoginEmail();
    }
}

/// <summary>UC-02 step 3 and E2: the formats of BR-17.</summary>
public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.FullName).FullName();
        RuleFor(x => x.Gender).NotNull().WithMessage("Choose the gender.").IsInEnum();
        RuleFor(x => x.Email).LoginEmail();
        RuleFor(x => x.Mobile).Mobile();
    }
}

/// <summary>Field rules of BR-17 that several account forms share.</summary>
public static class AccountRules
{
    /// <summary>Email: required, a valid address, up to 100 characters. An empty value only gets the first message.</summary>
    public static IRuleBuilderOptions<T, string> LoginEmail<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("Enter the email.")
            .MaximumLength(FieldLength.Email)
            .EmailAddress().WithMessage("Enter a valid email address.");

    /// <summary>Full name: 2 to 100 characters, not blank.</summary>
    public static IRuleBuilderOptions<T, string> FullName<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("Enter the full name.")
            // Blank is already told by NotEmpty; one message per problem.
            .Must(n => string.IsNullOrWhiteSpace(n) || n.Trim().Length is >= FieldLength.FullNameMin and <= FieldLength.FullName)
            .WithMessage($"The full name must have {FieldLength.FullNameMin} to {FieldLength.FullName} characters.");

    /// <summary>Mobile: 10 digits starting with 0.</summary>
    public static IRuleBuilderOptions<T, string> Mobile<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("Enter the mobile number.")
            .Matches("^0[0-9]{9}$").WithMessage("The mobile number must have 10 digits and start with 0.");
}
