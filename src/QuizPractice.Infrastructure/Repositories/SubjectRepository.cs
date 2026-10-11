using Microsoft.EntityFrameworkCore;
using QuizPractice.Domain.Common;
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

    public Task<Subject?> GetPublishedAsync(int id, CancellationToken ct = default) =>
        Published().FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<Page<Subject>> SearchPublishedAsync(PublishedSubjectSearch search, PageRequest page, CancellationToken ct = default)
    {
        var query = Published();

        if (!string.IsNullOrWhiteSpace(search.Text))
        {
            var text = search.Text.Trim();
            query = query.Where(s => s.Name.Contains(text));
        }

        if (search.CategoryId is { } categoryId)
        {
            query = query.Where(s => s.CategoryId == categoryId);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(s => s.UpdatedAt).ThenByDescending(s => s.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(ct);
        return new Page<Subject>(items, total, page.PageNumber, page.PageSize);
    }

    /// <summary>Published subjects with the category and only the Active packages, the ones a visitor can buy.</summary>
    private IQueryable<Subject> Published() =>
        db.Subjects.AsNoTracking()
            .Include(s => s.Category)
            .Include(s => s.PricePackages.Where(p => p.Status == ActiveStatus.Active))
            .Where(s => s.Status == SubjectStatus.Published);
}
