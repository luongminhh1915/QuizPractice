using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Sale;
using QuizPractice.Domain.Common;

namespace QuizPractice.Application.Services.Sale;

/// <summary>Invoices List and Invoice Details (UC-68, UC-69; team extension, OI-09), for the Sale role.</summary>
public interface IInvoiceService
{
    /// <summary>UC-68: every invoice, the unpaid ones first, then the newest first.</summary>
    Task<Result<Page<SaleInvoiceSummaryDto>>> ListAsync(InvoiceListRequest request, CancellationToken ct = default);

    /// <summary>UC-69 step 2.</summary>
    Task<Result<SaleInvoiceDetailsDto>> GetAsync(int id, CancellationToken ct = default);

    /// <summary>UC-69 steps 4 to 7: the invoice and every registration in it become Paid (BR-14).</summary>
    Task<Result<SaleInvoiceDetailsDto>> ConfirmAsync(int id, InvoiceNotesRequest request, CancellationToken ct = default);

    /// <summary>UC-69 A1–A3: the notes alone, at any status.</summary>
    Task<Result<SaleInvoiceDetailsDto>> SaveNotesAsync(int id, InvoiceNotesRequest request, CancellationToken ct = default);
}
