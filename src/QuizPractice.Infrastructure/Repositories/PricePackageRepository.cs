using Microsoft.EntityFrameworkCore;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Repositories;
using QuizPractice.Infrastructure.Persistence;

namespace QuizPractice.Infrastructure.Repositories;

public class PricePackageRepository(AppDbContext db) : IPricePackageRepository
{
    public async Task<IReadOnlyList<PricePackage>> ListAsync(int subjectId, CancellationToken ct = default) =>
        await db.PricePackages.AsNoTracking()
            .Where(p => p.SubjectId == subjectId)
            .OrderBy(p => p.AccessMonths).ThenBy(p => p.Id)
            .ToListAsync(ct);

    public Task<PricePackage?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.PricePackages.FirstOrDefaultAsync(p => p.Id == id, ct);

    public void Add(PricePackage package) => db.PricePackages.Add(package);
}
