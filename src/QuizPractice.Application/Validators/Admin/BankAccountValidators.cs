using FluentValidation;
using QuizPractice.Application.Dtos.Admin;
using QuizPractice.Application.Services.Payments;
using QuizPractice.Domain.Constants;

namespace QuizPractice.Application.Validators.Admin;

/// <summary>UC-70 step 4 and E1 (BR-17; the CHECK of SDS I.3: digits only).</summary>
public class SaveBankAccountRequestValidator : AbstractValidator<SaveBankAccountRequest>
{
    public SaveBankAccountRequestValidator()
    {
        RuleFor(x => x.BankBin).Must(b => VietQrBanks.Find(b ?? "") is not null).WithMessage("Choose the bank from the list.");
        RuleFor(x => x.AccountNumber)
            .NotEmpty().WithMessage("Enter the account number.")
            .Matches("^[0-9]+$").WithMessage("The account number is digits only.")
            .Length(6, FieldLength.AccountNumber).WithMessage($"The account number has 6 to {FieldLength.AccountNumber} digits.");
        RuleFor(x => x.AccountName)
            .NotEmpty().WithMessage("Enter the account holder.")
            .MaximumLength(FieldLength.AccountName);
    }
}
