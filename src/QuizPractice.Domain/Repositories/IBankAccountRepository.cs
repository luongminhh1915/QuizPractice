using QuizPractice.Domain.Entities;

namespace QuizPractice.Domain.Repositories;

/// <summary>The one receiving bank account (BR-24, UC-70).</summary>
public interface IBankAccountRepository
{
    /// <summary>The account, tracked for a change; null when none has been set yet.</summary>
    Task<BankAccount?> GetAsync(CancellationToken ct = default);

    /// <summary>Stages the first account (id 1); the service commits through IUnitOfWork.</summary>
    void Add(BankAccount account);
}
