using Microsoft.EntityFrameworkCore;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
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

    public async Task<Page<Invoice>> ListByUserAsync(int userId, InvoiceStatus? status, PageRequest page, CancellationToken ct = default)
    {
        var query = db.Invoices.AsNoTracking().Where(i => i.UserId == userId);
        if (status is { } s)
        {
            query = query.Where(i => i.Status == s);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .Include(i => i.Registrations)
            .OrderByDescending(i => i.CreatedAt).ThenByDescending(i => i.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(ct);
        return new Page<Invoice>(items, total, page.PageNumber, page.PageSize);
    }

    public void Add(Invoice invoice) => db.Invoices.Add(invoice);
}
