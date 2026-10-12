using QuizPractice.Domain.Common;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Domain.Repositories;

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

    /// <summary>Stages a new invoice; the service commits through IUnitOfWork.</summary>
    void Add(Invoice invoice);
}
