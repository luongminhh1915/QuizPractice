using FluentValidation;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Sale;
using QuizPractice.Application.Validators;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.Application.Services.Sale;

/// <summary>
/// The Sale side of the payment by QR code (BR-19, BR-24): the system never sees the
/// transfer, so the Sale user finds it on the receiving account and settles the invoice here.
/// </summary>
public class InvoiceService(
    IInvoiceRepository invoices,
    IValidator<InvoiceListRequest> listValidator) : IInvoiceService
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
}
