using QuizPractice.Domain.Common;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Domain.Repositories;

/// <summary>
/// Criteria of Invoices List (UC-68): text matched against the transfer code and the
/// customer email, one status, and a creation-time range in UTC (From included, Before excluded).
/// </summary>
public sealed record InvoiceSearch(string? Text, InvoiceStatus? Status, DateTime? From, DateTime? Before);

/// <summary>Invoices of the payment by QR code (UC-18, UC-67; the Sale screens UC-68, UC-69 extend it).</summary>
public interface IInvoiceRepository
{
    /// <summary>The one receiving bank account (BR-24), or null when it has not been seeded or set.</summary>
    Task<BankAccount?> GetBankAccountAsync(CancellationToken ct = default);

    Task<bool> CodeExistsAsync(string code, CancellationToken ct = default);

    /// <summary>
    /// One invoice of the account, tracked, with its registrations (subject, package);
    /// null when it is missing or belongs to someone else.
    /// </summary>
    Task<Invoice?> GetOwnAsync(int id, int userId, CancellationToken ct = default);

    /// <summary>UC-67: a page of the account's invoices, the newest first, with their registrations; optionally one status.</summary>
    Task<Page<Invoice>> ListByUserAsync(int userId, InvoiceStatus? status, PageRequest page, CancellationToken ct = default);

    /// <summary>
    /// UC-68: a page of every invoice with its customer, its registrations and the Sale user
    /// who confirmed it; the unpaid ones first, then the newest first.
    /// </summary>
    Task<Page<Invoice>> SearchAsync(InvoiceSearch search, PageRequest page, CancellationToken ct = default);

    /// <summary>
    /// UC-69: one invoice, tracked, with its customer, the Sale user who confirmed it and its
    /// registrations (subject, package); null when it does not exist.
    /// </summary>
    Task<Invoice?> GetForSaleAsync(int id, CancellationToken ct = default);

    /// <summary>Stages a new invoice; the service commits through IUnitOfWork.</summary>
    void Add(Invoice invoice);
}
