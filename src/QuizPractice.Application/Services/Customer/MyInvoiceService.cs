using System.Security.Cryptography;
using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Customer;
using QuizPractice.Application.Services.Payments;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.Application.Services.Customer;

/// <summary>
/// Payment by QR code (UC-18, team extension, OI-09). The system never sees the transfer:
/// it only shows the request, and the Sale role confirms the invoice after checking the
/// receiving account (BR-19, UC-69). A registration sits in at most one Unpaid invoice;
/// it keeps pointing at a cancelled one, which no longer holds it (BR-24).
/// </summary>
public class MyInvoiceService(
    IInvoiceRepository invoices,
    IRegistrationRepository registrations,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser) : IMyInvoiceService
{
    /// <summary>One transfer for a handful of registrations; a larger list is surely a mistake.</summary>
    public const int MaxRegistrations = 20;

    public async Task<Result<InvoiceDto>> CreateAsync(CreateInvoiceRequest request, CancellationToken ct = default)
    {
        if (currentUser.Id is not { } userId)
        {
            return CustomerAccess.SignInRequired;
        }

        var ids = (request.RegistrationIds ?? []).Distinct().ToList();
        if (ids.Count is 0 or > MaxRegistrations)
        {
            return Error.Invalid(nameof(request.RegistrationIds), $"Choose 1 to {MaxRegistrations} registrations to pay.");
        }

        // Step 2 and E1: each one the customer's, still Submitted, in no unpaid invoice.
        var chosen = await registrations.ListOwnByIdsAsync(userId, ids, ct);
        foreach (var id in ids)
        {
            var r = chosen.FirstOrDefault(x => x.Id == id);
            if (r is null)
            {
                return CustomerAccess.NotFound("Registration", id);
            }

            if (r.Status != RegistrationStatus.Submitted || r.Invoice is { Status: InvoiceStatus.Unpaid })
            {
                return NotPayable(r);
            }
        }

        // E2: no receiving account to send the money to.
        var bank = await invoices.GetBankAccountAsync(ct);
        if (bank is null || string.IsNullOrWhiteSpace(bank.AccountNumber) || string.IsNullOrWhiteSpace(bank.BankBin))
        {
            return Error.Conflict("Invoice.NoBankAccount", "Payment by QR code is not available right now. Please try again later.");
        }

        // Steps 3 to 5: the total, a code of its own and a copy of the account (BR-24).
        var invoice = new Invoice
        {
            UserId = userId,
            Code = await NewCodeAsync(ct),
            TotalAmount = chosen.Sum(r => r.TotalCost),
            BankName = bank.BankName,
            BankBin = bank.BankBin,
            AccountNumber = bank.AccountNumber,
            AccountName = bank.AccountName,
            Status = InvoiceStatus.Unpaid,
            CreatedAt = NowToTheSecond(),
        };
        foreach (var r in chosen)
        {
            r.Invoice = invoice;
            r.UpdatedById = userId;
            invoice.Registrations.Add(r);
        }

        invoices.Add(invoice);

        // A registration a Sale user settled after it was read fails the save (E1).
        if (!await unitOfWork.TrySaveChangesAsync(ct))
        {
            return Error.Conflict("Invoice.RegistrationNotPayable",
                "One of the registrations was settled in the meantime. Reload My Registrations and choose again.");
        }

        // Step 6: the QR code.
        return InvoiceDto.From(invoice, QrOf(invoice));
    }

    public async Task<Result<InvoiceDto>> GetAsync(int id, CancellationToken ct = default)
    {
        if (currentUser.Id is not { } userId)
        {
            return CustomerAccess.SignInRequired;
        }

        var invoice = await invoices.GetOwnAsync(id, userId, ct);
        return invoice is null ? CustomerAccess.NotFound("Invoice", id) : InvoiceDto.From(invoice, QrOf(invoice));
    }

    public async Task<Result<InvoiceDto>> CancelAsync(int id, CancellationToken ct = default)
    {
        if (currentUser.Id is not { } userId)
        {
            return CustomerAccess.SignInRequired;
        }

        var invoice = await invoices.GetOwnAsync(id, userId, ct);
        if (invoice is null)
        {
            return CustomerAccess.NotFound("Invoice", id);
        }

        if (invoice.Status != InvoiceStatus.Unpaid)
        {
            return InvoiceClosed(invoice);
        }

        // The registrations keep their link but are free again: only an Unpaid invoice holds them.
        invoice.Status = InvoiceStatus.Cancelled;

        // A Sale user who confirmed it after it was read wins (the status is a concurrency token).
        if (!await unitOfWork.TrySaveChangesAsync(ct))
        {
            return Error.Conflict("Invoice.Closed", "This invoice was confirmed by the Sale team in the meantime.");
        }

        return InvoiceDto.From(invoice, null);
    }

    /// <summary>The column keeps seconds; the answer shows the time as it is stored.</summary>
    private static DateTime NowToTheSecond()
    {
        var now = DateTime.UtcNow;
        return new DateTime(now.Ticks - now.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
    }

    /// <summary>The VietQR text, only for an invoice still waiting for the transfer.</summary>
    private static string? QrOf(Invoice i) =>
        i.Status == InvoiceStatus.Unpaid ? VietQrBuilder.Build(i.BankBin, i.AccountNumber, i.TotalAmount, i.Code) : null;

    /// <summary>"QPS" + 6 upper-case hex characters, drawn again on a clash (SDS I.3, Invoices.code).</summary>
    private async Task<string> NewCodeAsync(CancellationToken ct)
    {
        while (true)
        {
            var code = "QPS" + Convert.ToHexString(RandomNumberGenerator.GetBytes(3));
            if (!await invoices.CodeExistsAsync(code, ct))
            {
                return code;
            }
        }
    }

    private static Error NotPayable(Registration r) =>
        Error.Conflict("Invoice.RegistrationNotPayable",
            $"Registration {r.Id} ({r.Subject.Name}) is no longer Submitted or is already in an unpaid invoice. Reload My Registrations and choose again.");

    private static Error InvoiceClosed(Invoice i) =>
        Error.Conflict("Invoice.Closed", $"Invoice {i.Code} is already {i.Status}.");
}
