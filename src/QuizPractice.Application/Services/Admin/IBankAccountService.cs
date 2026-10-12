using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Admin;

namespace QuizPractice.Application.Services.Admin;

/// <summary>UC-70 Edit Receiving Bank Account (Admin only).</summary>
public interface IBankAccountService
{
    Task<Result<BankAccountDto>> GetAsync(CancellationToken ct = default);

    /// <summary>409 BankAccount.Changed when another Admin saved since the screen opened (E2).</summary>
    Task<Result<BankAccountSavedDto>> SaveAsync(SaveBankAccountRequest request, CancellationToken ct = default);

    /// <summary>Step 6: a VietQR code for 10,000 VND to the account as entered, to scan before saving.</summary>
    Task<Result<SampleQrDto>> SampleQrAsync(string? bankBin, string? accountNumber, CancellationToken ct = default);
}
