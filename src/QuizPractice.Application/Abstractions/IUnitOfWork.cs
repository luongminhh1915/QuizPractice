namespace QuizPractice.Application.Abstractions;

/// <summary>
/// Commits everything the repositories staged, in one transaction. A service calls it
/// once per use case. AppDbContext implements it, so Application never references
/// Infrastructure (SDS II, design note of every sequence diagram).
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Runs work that must commit in more than one step inside one transaction: either
    /// every step is stored or none is. Needed when a unique rule would be broken
    /// half-way through one SaveChanges, e.g. moving the single correct option of a
    /// question from one row to another.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> work, CancellationToken ct = default);
}
