using FluentValidation;
using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Sale;
using QuizPractice.Application.Services.TestContent;
using QuizPractice.Application.Validators;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.Application.Services.Sale;

/// <summary>
/// The Sale side of the payment by QR code (BR-19, BR-24): the system never sees the
/// transfer, so the Sale user finds it on the receiving account and settles the invoice here.
/// </summary>
public class InvoiceService(
    IInvoiceRepository invoices,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IValidator<InvoiceListRequest> listValidator,
    IValidator<InvoiceNotesRequest> notesValidator) : IInvoiceService
{
    public async Task<Result<Page<SaleInvoiceSummaryDto>>> ListAsync(InvoiceListRequest request, CancellationToken ct = default)
    {
        if (await listValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        var search = new InvoiceSearch(
            request.Search,
            request.Status,
            request.From is { } from ? VietnamTime.StartUtc(from) : null,
            request.To is { } to ? VietnamTime.StartUtc(to.AddDays(1)) : null);
        var page = await invoices.SearchAsync(search, new PageRequest(request.Page, request.PageSize), ct);
        return page.Map(SaleInvoiceSummaryDto.From);
    }

    public async Task<Result<SaleInvoiceDetailsDto>> GetAsync(int id, CancellationToken ct = default)
    {
        var invoice = await invoices.GetForSaleAsync(id, ct);
        return invoice is null ? Error.NotFound("Invoice", id) : SaleInvoiceDetailsDto.From(invoice);
    }

    public async Task<Result<SaleInvoiceDetailsDto>> ConfirmAsync(int id, InvoiceNotesRequest request, CancellationToken ct = default)
    {
        if (currentUser.Id is not { } me)
        {
            return SignInRequired;
        }

        if (await notesValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        var invoice = await invoices.GetForSaleAsync(id, ct);
        if (invoice is null)
        {
            return Error.NotFound("Invoice", id);
        }

        // E1: the customer cancelled it; E3 / A3: it is settled already.
        if (invoice.Status == InvoiceStatus.Cancelled)
        {
            return Error.Conflict("Invoice.Cancelled", $"The customer has cancelled invoice {invoice.Code}; it cannot be confirmed.");
        }

        if (invoice.Status == InvoiceStatus.Paid)
        {
            return Error.Conflict("Invoice.Paid", $"Invoice {invoice.Code} has already been confirmed by {invoice.ConfirmedBy?.Email ?? "another Sale user"}.");
        }

        // E2: every registration must be able to open its subject before anything changes.
        foreach (var registration in invoice.Registrations)
        {
            if (RegistrationSettlement.CheckPayable(registration) is { } noDuration)
            {
                return noDuration;
            }
        }

        // Steps 5 to 7: one save for the invoice and its registrations (BR-14, BR-22).
        var now = DateTime.UtcNow;
        foreach (var registration in invoice.Registrations.Where(r => r.Status == RegistrationStatus.Submitted))
        {
            RegistrationSettlement.MarkPaid(registration, me, now);
        }

        invoice.Status = InvoiceStatus.Paid;
        invoice.PaidAt = now;
        invoice.ConfirmedById = me;
        invoice.Notes = QuestionMapping.NullIfBlank(request.Notes) ?? invoice.Notes;

        // E1, E3 at the last moment: the invoice status is a concurrency token, so a
        // cancellation or another confirmation in the meantime makes this one fail.
        if (!await unitOfWork.TrySaveChangesAsync(ct))
        {
            return ChangedMeanwhile(invoice.Code);
        }

        return SaleInvoiceDetailsDto.From((await invoices.GetForSaleAsync(id, ct))!);
    }

    public async Task<Result<SaleInvoiceDetailsDto>> SaveNotesAsync(int id, InvoiceNotesRequest request, CancellationToken ct = default)
    {
        if (await notesValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        var invoice = await invoices.GetForSaleAsync(id, ct);
        if (invoice is null)
        {
            return Error.NotFound("Invoice", id);
        }

        invoice.Notes = QuestionMapping.NullIfBlank(request.Notes);
        if (!await unitOfWork.TrySaveChangesAsync(ct))
        {
            return ChangedMeanwhile(invoice.Code);
        }

        return SaleInvoiceDetailsDto.From((await invoices.GetForSaleAsync(id, ct))!);
    }

    private static Error SignInRequired => new(ErrorType.Unauthorized, "Auth.Required", "Log in as a Sale user.");

    private static Error ChangedMeanwhile(string code) =>
        Error.Conflict("Invoice.Changed", $"Invoice {code} has changed since it was opened. It is reloaded with its current status.");
}
