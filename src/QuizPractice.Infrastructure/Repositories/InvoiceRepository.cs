using Microsoft.EntityFrameworkCore;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Repositories;
using QuizPractice.Infrastructure.Persistence;

namespace QuizPractice.Infrastructure.Repositories;

public class InvoiceRepository(AppDbContext db) : IInvoiceRepository
{
    public Task<BankAccount?> GetBankAccountAsync(CancellationToken ct = default) =>
        db.BankAccounts.AsNoTracking().FirstOrDefaultAsync(b => b.Id == BankAccount.SingleId, ct);

    public Task<bool> CodeExistsAsync(string code, CancellationToken ct = default) =>
        db.Invoices.AnyAsync(i => i.Code == code, ct);

    public Task<Invoice?> GetOwnAsync(int id, int userId, CancellationToken ct = default) =>
        db.Invoices
            .Include(i => i.Registrations).ThenInclude(r => r.Subject)
            .Include(i => i.Registrations).ThenInclude(r => r.Package)
            .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId, ct);

    public void Add(Invoice invoice) => db.Invoices.Add(invoice);
}
