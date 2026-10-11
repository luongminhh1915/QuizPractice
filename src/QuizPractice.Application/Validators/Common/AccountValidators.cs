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

/// <summary>Field rules of BR-17 that several account forms share.</summary>
public static class AccountRules
{
    /// <summary>Email: required, a valid address, up to 100 characters. An empty value only gets the first message.</summary>
    public static IRuleBuilderOptions<T, string> LoginEmail<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("Enter the email.")
            .MaximumLength(FieldLength.Email)
            .EmailAddress().WithMessage("Enter a valid email address.");
}
