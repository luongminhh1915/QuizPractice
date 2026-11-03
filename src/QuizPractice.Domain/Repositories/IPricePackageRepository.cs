using QuizPractice.Domain.Entities;

namespace QuizPractice.Domain.Repositories;

/// <summary>The price packages of a subject as the Price Package tab manages them (UC-47, UC-48), whatever their status.</summary>
public interface IPricePackageRepository
{
    /// <summary>The packages of a subject, shortest access first.</summary>
    Task<IReadOnlyList<PricePackage>> ListAsync(int subjectId, CancellationToken ct = default);

    /// <summary>One package, tracked for a change.</summary>
    Task<PricePackage?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>Whether another package of the subject already has this name, whatever its status.</summary>
    Task<bool> NameExistsAsync(int subjectId, string name, int? exceptId, CancellationToken ct = default);

    void Add(PricePackage package);
}
