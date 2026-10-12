using Microsoft.EntityFrameworkCore;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Constants;
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

    public async Task<Page<Subject>> SearchManagedAsync(ManagedSubjectSearch search, PageRequest page, CancellationToken ct = default)
    {
        var query = db.Subjects.AsNoTracking().AsQueryable();

        // BR-11: an Expert never receives a subject of another Expert, whatever the filters say.
        if (search.ExpertId is { } expertId)
        {
            query = query.Where(s => s.ExpertId == expertId);
        }

        if (!string.IsNullOrWhiteSpace(search.Text))
        {
            var text = search.Text.Trim();
            query = query.Where(s => s.Name.Contains(text));
        }

        if (search.CategoryId is { } categoryId)
        {
            query = query.Where(s => s.CategoryId == categoryId);
        }

        if (search.Status is { } status)
        {
            query = query.Where(s => s.Status == status);
        }

        var total = await query.CountAsync(ct);
        var items = await Sort(query, search.SortBy, search.Descending)
            .Include(s => s.Category)
            .Include(s => s.Expert)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(ct);
        return new Page<Subject>(items, total, page.PageNumber, page.PageSize);
    }

    public async Task<IReadOnlyDictionary<int, int>> CountLessonsAsync(IReadOnlyCollection<int> subjectIds, CancellationToken ct = default)
    {
        var ids = subjectIds.ToList();
        return await Lessons()
            .Where(l => ids.Contains(l.SubjectId))
            .GroupBy(l => l.SubjectId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);
    }

    public Task<Subject?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.Subjects
            .Include(s => s.Category)
            .Include(s => s.Expert)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public void Add(Subject subject) => db.Subjects.Add(subject);

    public async Task<IReadOnlyList<Subject>> ListOwnedByAsync(int userId, CancellationToken ct = default) =>
        await db.Subjects.Where(s => s.ExpertId == userId).OrderBy(s => s.Name).ToListAsync(ct);

    public Task<bool> HasActivePackageAsync(int subjectId, CancellationToken ct = default) =>
        db.PricePackages.AnyAsync(p => p.SubjectId == subjectId && p.Status == ActiveStatus.Active, ct);

    /// <summary>Lessons and quizzes of every subject; a Subject Topic only groups them and is not counted.</summary>
    private IQueryable<Lesson> Lessons() =>
        db.Lessons.Where(l => l.Type.Value != SettingValues.LessonTypes.SubjectTopic);

    private IQueryable<Subject> Sort(IQueryable<Subject> query, string? sortBy, bool descending)
    {
        // A captured query, not a method call, so EF turns the count into a subquery.
        var lessons = Lessons();
        IOrderedQueryable<Subject> ordered = sortBy?.ToLowerInvariant() switch
        {
            "id" => descending ? query.OrderByDescending(s => s.Id) : query.OrderBy(s => s.Id),
            "name" => descending ? query.OrderByDescending(s => s.Name) : query.OrderBy(s => s.Name),
            "category" => descending ? query.OrderByDescending(s => s.Category.Value) : query.OrderBy(s => s.Category.Value),
            "lessons" => descending
                ? query.OrderByDescending(s => lessons.Count(l => l.SubjectId == s.Id))
                : query.OrderBy(s => lessons.Count(l => l.SubjectId == s.Id)),
            "owner" => descending ? query.OrderByDescending(s => s.Expert!.FullName) : query.OrderBy(s => s.Expert!.FullName),
            "status" => descending ? query.OrderByDescending(s => s.Status) : query.OrderBy(s => s.Status),
            _ => query.OrderByDescending(s => s.Id),
        };
        return descending || sortBy is null ? ordered.ThenByDescending(s => s.Id) : ordered.ThenBy(s => s.Id);
    }

    /// <summary>Published subjects with the category and only the Active packages, the ones a visitor can buy.</summary>
    private IQueryable<Subject> Published() =>
        db.Subjects.AsNoTracking()
            .Include(s => s.Category)
            .Include(s => s.PricePackages.Where(p => p.Status == ActiveStatus.Active))
            .Where(s => s.Status == SubjectStatus.Published);
}
