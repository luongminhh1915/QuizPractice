using Microsoft.EntityFrameworkCore;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;
using QuizPractice.Infrastructure.Persistence;

namespace QuizPractice.Infrastructure.Repositories;

public class SubjectRepository(AppDbContext db) : ISubjectRepository
{
    public async Task<IReadOnlyList<Subject>> ListFeaturedPublishedAsync(int take, CancellationToken ct = default) =>
        await Published()
            .Where(s => s.IsFeatured)
            .OrderByDescending(s => s.UpdatedAt).ThenByDescending(s => s.Id)
            .Take(take)
            .ToListAsync(ct);

    /// <summary>Published subjects with the category and only the Active packages, the ones a visitor can buy.</summary>
    private IQueryable<Subject> Published() =>
        db.Subjects.AsNoTracking()
            .Include(s => s.Category)
            .Include(s => s.PricePackages.Where(p => p.Status == ActiveStatus.Active))
            .Where(s => s.Status == SubjectStatus.Published);
}
