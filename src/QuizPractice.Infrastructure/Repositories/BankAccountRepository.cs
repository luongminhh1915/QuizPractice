using Microsoft.EntityFrameworkCore;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Repositories;
using QuizPractice.Infrastructure.Persistence;

namespace QuizPractice.Infrastructure.Repositories;

public class BankAccountRepository(AppDbContext db) : IBankAccountRepository
{
    public Task<BankAccount?> GetAsync(CancellationToken ct = default) =>
        db.BankAccounts.FirstOrDefaultAsync(b => b.Id == BankAccount.SingleId, ct);

    public void Add(BankAccount account) => db.BankAccounts.Add(account);
}
