using QuizPractice.Domain.Entities;

namespace QuizPractice.Application.Dtos.Admin;

/// <summary>
/// Bank Account Settings (UC-70 step 3). Version: the last change the screen was opened
/// with, so a save over another Admin's change is refused (E2); empty when no account exists yet.
/// </summary>
public sealed record SaveBankAccountRequest(string BankBin, string AccountNumber, string AccountName, string? Version);

public sealed record BankDto(string Bin, string ShortName, string Name);

/// <summary>
/// The stored account (or nulls when none is set), when it last changed, and the banks of
/// the VietQR list. The SRS also names the Admin who changed it; the table has no column
/// for that (Duck, 12/10), so only the time is shown.
/// </summary>
public sealed record BankAccountDto(
    string? BankBin,
    string? BankName,
    string? AccountNumber,
    string? AccountName,
    DateTime? UpdatedAt,
    string? Version,
    IReadOnlyList<BankDto> Banks)
{
    public static string VersionOf(BankAccount a) => a.UpdatedAt.Ticks.ToString();
}

/// <summary>After Save: Changed false when nothing differed and nothing was stored (A2).</summary>
public sealed record BankAccountSavedDto(BankAccountDto Account, bool Changed);

/// <summary>Step 6: the text of a sample VietQR code for a small amount, drawn by the page.</summary>
public sealed record SampleQrDto(string Payload, decimal Amount, string Content);
