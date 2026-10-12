using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Application.Dtos.Sale;

/// <summary>Criteria of Invoices List (UC-68). From and To are Vietnam dates of the creation time, both included.</summary>
public sealed record InvoiceListRequest(
    string? Search = null,
    InvoiceStatus? Status = null,
    DateOnly? From = null,
    DateOnly? To = null,
    int Page = 1,
    int PageSize = 10);

/// <summary>One registration an invoice pays for, with its status (SRS III 8.4, Registrations).</summary>
public sealed record SaleInvoiceLineDto(int Id, string SubjectName, string PackageName, decimal TotalCost, RegistrationStatus Status);

/// <summary>
/// Invoice Details (SRS III 8.4; UC-69). The receiving account is the copy the customer was
/// shown (BR-24): the Sale user looks for this code and amount on that account.
/// </summary>
public sealed record SaleInvoiceDetailsDto(
    int Id,
    string Code,
    InvoiceStatus Status,
    DateTime CreatedAt,
    DateTime? PaidAt,
    string? ConfirmedBy,
    string CustomerName,
    string CustomerEmail,
    string BankName,
    string AccountNumber,
    string AccountName,
    decimal TotalAmount,
    string? Notes,
    IReadOnlyList<SaleInvoiceLineDto> Registrations)
{
    public static SaleInvoiceDetailsDto From(Invoice i) =>
        new(i.Id, i.Code, i.Status, DateTime.SpecifyKind(i.CreatedAt, DateTimeKind.Utc),
            i.PaidAt is { } paid ? DateTime.SpecifyKind(paid, DateTimeKind.Utc) : null, i.ConfirmedBy?.Email,
            i.User.FullName, i.User.Email, i.BankName, i.AccountNumber, i.AccountName, i.TotalAmount, i.Notes,
            i.Registrations.OrderBy(r => r.Id)
                .Select(r => new SaleInvoiceLineDto(r.Id, r.Subject.Name, r.Package.Name, r.TotalCost, r.Status)).ToList());
}

/// <summary>UC-69 step 4, A1–A3: the notes of the Sale user, such as where the transfer was found.</summary>
public sealed record InvoiceNotesRequest(string? Notes);

/// <summary>One row of Invoices List (SRS III 8.3).</summary>
public sealed record SaleInvoiceSummaryDto(
    int Id,
    string Code,
    string CustomerEmail,
    DateTime CreatedAt,
    int RegistrationCount,
    decimal TotalAmount,
    InvoiceStatus Status,
    DateTime? PaidAt,
    string? ConfirmedBy)
{
    public static SaleInvoiceSummaryDto From(Invoice i) =>
        new(i.Id, i.Code, i.User.Email, DateTime.SpecifyKind(i.CreatedAt, DateTimeKind.Utc), i.Registrations.Count, i.TotalAmount,
            i.Status, i.PaidAt is { } paid ? DateTime.SpecifyKind(paid, DateTimeKind.Utc) : null, i.ConfirmedBy?.Email);
}
