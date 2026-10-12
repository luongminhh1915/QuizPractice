using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Application.Dtos.Customer;

/// <summary>UC-18 step 1: the ticked registrations to pay with one transfer.</summary>
public sealed record CreateInvoiceRequest(IReadOnlyList<int> RegistrationIds);

/// <summary>Criteria of My Invoices (UC-67): one status or all, the newest first.</summary>
public sealed record MyInvoiceListRequest(InvoiceStatus? Status = null, int Page = 1, int PageSize = 10);

/// <summary>One row of My Invoices (SRS III 3.5).</summary>
public sealed record InvoiceSummaryDto(int Id, string Code, DateTime CreatedAt, int RegistrationCount, decimal TotalAmount, InvoiceStatus Status, DateTime? PaidAt)
{
    public static InvoiceSummaryDto From(Invoice i) =>
        new(i.Id, i.Code, DateTime.SpecifyKind(i.CreatedAt, DateTimeKind.Utc), i.Registrations.Count, i.TotalAmount, i.Status,
            i.PaidAt is { } paid ? DateTime.SpecifyKind(paid, DateTimeKind.Utc) : null);
}

/// <summary>One registration an invoice pays for (SRS III 2.7, Registrations).</summary>
public sealed record InvoiceLineDto(int Id, string SubjectName, string PackageName, decimal TotalCost);

/// <summary>
/// Invoice QR (SRS III 2.7). The bank fields are the copy taken when the invoice was made
/// (BR-24). QrPayload is the VietQR text, only while the invoice is Unpaid; a Paid or
/// Cancelled invoice opens read only (UC-67 A2).
/// </summary>
public sealed record InvoiceDto(
    int Id,
    string Code,
    InvoiceStatus Status,
    DateTime CreatedAt,
    DateTime? PaidAt,
    decimal TotalAmount,
    string BankName,
    string BankBin,
    string AccountNumber,
    string AccountName,
    string? QrPayload,
    IReadOnlyList<InvoiceLineDto> Registrations)
{
    public static InvoiceDto From(Invoice i, string? qrPayload) =>
        new(i.Id, i.Code, i.Status,
            DateTime.SpecifyKind(i.CreatedAt, DateTimeKind.Utc),
            i.PaidAt is { } paid ? DateTime.SpecifyKind(paid, DateTimeKind.Utc) : null,
            i.TotalAmount, i.BankName, i.BankBin, i.AccountNumber, i.AccountName, qrPayload,
            i.Registrations.OrderBy(r => r.Id).Select(r => new InvoiceLineDto(r.Id, r.Subject.Name, r.Package.Name, r.TotalCost)).ToList());
}
