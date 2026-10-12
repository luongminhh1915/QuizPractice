using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Sale;
using QuizPractice.Domain.Common;

namespace QuizPractice.Application.Services.Sale;

/// <summary>Invoices List and Invoice Details (UC-68, UC-69; team extension, OI-09), for the Sale role.</summary>
public interface IInvoiceService
{
    /// <summary>UC-68: every invoice, the unpaid ones first, then the newest first.</summary>
    Task<Result<Page<SaleInvoiceSummaryDto>>> ListAsync(InvoiceListRequest request, CancellationToken ct = default);
}
