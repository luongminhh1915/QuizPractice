using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Customer;

namespace QuizPractice.Application.Services.Customer;

/// <summary>UC-18 Pay by QR Code, UC-67 View My Invoices: the customer's own invoices only.</summary>
public interface IMyInvoiceService
{
    /// <summary>UC-18: one Unpaid invoice for the chosen Submitted registrations, with its QR code.</summary>
    Task<Result<InvoiceDto>> CreateAsync(CreateInvoiceRequest request, CancellationToken ct = default);

    /// <summary>Invoice QR (2.7): the QR code only while the invoice is Unpaid.</summary>
    Task<Result<InvoiceDto>> GetAsync(int id, CancellationToken ct = default);

    /// <summary>UC-18 A2: an Unpaid invoice is cancelled and its registrations are free again (BR-24).</summary>
    Task<Result<InvoiceDto>> CancelAsync(int id, CancellationToken ct = default);
}
