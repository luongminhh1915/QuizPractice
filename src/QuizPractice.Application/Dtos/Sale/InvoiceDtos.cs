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
