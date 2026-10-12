using System.Globalization;
using System.Text;
using FluentValidation;
using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Admin;
using QuizPractice.Application.Services.Payments;
using QuizPractice.Application.Validators;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.Application.Services.Admin;

public class BankAccountService(
    IBankAccountRepository accounts,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IValidator<SaveBankAccountRequest> saveValidator) : IBankAccountService
{
    /// <summary>Step 6: a small amount, enough for the banking app to show the account name.</summary>
    public const decimal SampleAmount = 10_000m;

    private const string SampleContent = "QPS TEST";

    public async Task<Result<BankAccountDto>> GetAsync(CancellationToken ct = default)
    {
        if (AdminAccess.Check(currentUser) is { } denied)
        {
            return denied;
        }

        return ToDto(await accounts.GetAsync(ct));
    }

    public async Task<Result<BankAccountSavedDto>> SaveAsync(SaveBankAccountRequest request, CancellationToken ct = default)
    {
        if (AdminAccess.Check(currentUser) is { } denied)
        {
            return denied;
        }

        if (await saveValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        var account = await accounts.GetAsync(ct);

        // E2: another Admin saved since the screen was opened (or set the first account meanwhile).
        if ((account is null ? null : BankAccountDto.VersionOf(account)) != NullIfBlank(request.Version))
        {
            return Changed;
        }

        var bank = VietQrBanks.Find(request.BankBin)!;
        var number = request.AccountNumber.Trim();
        var holder = Holder(request.AccountName);

        // A2: nothing differs, so nothing is stored.
        if (account is not null && account.BankBin == bank.Bin && account.AccountNumber == number && account.AccountName == holder)
        {
            return new BankAccountSavedDto(ToDto(account), Changed: false);
        }

        if (account is null)
        {
            account = new BankAccount();
            accounts.Add(account);
        }

        account.BankBin = bank.Bin;
        account.BankName = bank.ShortName;
        account.AccountNumber = number;
        account.AccountName = holder;

        // datetime2(0): whole seconds, so the version read back later is the same one.
        var now = DateTime.UtcNow;
        account.UpdatedAt = new DateTime(now.Ticks - now.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);

        // The concurrency token catches a save that lands between the read and this write.
        if (!await unitOfWork.TrySaveChangesAsync(ct))
        {
            return Changed;
        }

        // Invoices keep their own copy of the account: only new ones use this (BR-24).
        return new BankAccountSavedDto(ToDto(account), Changed: true);
    }

    public Task<Result<SampleQrDto>> SampleQrAsync(string? bankBin, string? accountNumber, CancellationToken ct = default)
    {
        if (AdminAccess.Check(currentUser) is { } denied)
        {
            return Task.FromResult<Result<SampleQrDto>>(denied);
        }

        var number = accountNumber?.Trim() ?? string.Empty;
        if (VietQrBanks.Find(bankBin ?? string.Empty) is null || number.Length is < 6 or > 19 || !number.All(char.IsAsciiDigit))
        {
            return Task.FromResult<Result<SampleQrDto>>(
                Error.Invalid("AccountNumber", "Choose the bank and enter a valid account number to see the sample QR code."));
        }

        var payload = VietQrBuilder.Build(bankBin!, number, SampleAmount, SampleContent);
        return Task.FromResult<Result<SampleQrDto>>(new SampleQrDto(payload, SampleAmount, SampleContent));
    }

    /// <summary>The holder as banking apps show it: capitals without accents (SRS III 9.5).</summary>
    public static string Holder(string name)
    {
        var plain = new StringBuilder();
        foreach (var c in name.Trim().Replace('đ', 'd').Replace('Đ', 'D').Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                plain.Append(c);
            }
        }

        return string.Join(' ', plain.ToString().ToUpperInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static BankAccountDto ToDto(BankAccount? a) =>
        // Stored as UTC; the kind is set so the page receives "...Z" and shows Vietnam time.
        new(a?.BankBin, a?.BankName, a?.AccountNumber, a?.AccountName,
            a is null ? null : DateTime.SpecifyKind(a.UpdatedAt, DateTimeKind.Utc),
            a is null ? null : BankAccountDto.VersionOf(a),
            VietQrBanks.All.Select(b => new BankDto(b.Bin, b.ShortName, b.Name)).ToList());

    private static string? NullIfBlank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;

    private static readonly Error Changed = Error.Conflict("BankAccount.Changed",
        "Another Admin changed the receiving account meanwhile. The current values are shown; make your change again.");
}
