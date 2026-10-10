using Microsoft.EntityFrameworkCore;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;
using QuizPractice.Infrastructure.Persistence;

namespace QuizPractice.Infrastructure.Repositories;

public class QuizRepository(AppDbContext db) : IQuizRepository
{
    public Task<Quiz?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.Quizzes.Include(q => q.Subject).FirstOrDefaultAsync(q => q.Id == id, ct);

    public async Task<IReadOnlyList<string>> GetLessonsUsingAsync(int quizId, CancellationToken ct = default) =>
        await db.Lessons.Where(l => l.QuizId == quizId).OrderBy(l => l.Name).Select(l => l.Name).ToListAsync(ct);

    public void Remove(Quiz quiz) => db.Quizzes.Remove(quiz);

    public async Task<Page<Quiz>> SearchAsync(QuizSearch search, PageRequest page, CancellationToken ct = default)
    {
        var query = db.Quizzes.AsNoTracking().AsQueryable();

        // BR-11: an Expert never receives a quiz of another subject, whatever the filters say.
        if (search.ExpertId is { } expertId)
        {
            query = query.Where(q => q.Subject.ExpertId == expertId);
        }

        if (!string.IsNullOrWhiteSpace(search.Text))
        {
            var text = search.Text.Trim();
            query = query.Where(q => q.Name.Contains(text));
        }

        if (search.SubjectId is { } subjectId)
        {
            query = query.Where(q => q.SubjectId == subjectId);
        }

        if (search.TypeId is { } typeId)
        {
            query = query.Where(q => q.TypeId == typeId);
        }

        var total = await query.CountAsync(ct);
        var items = await Sort(query, search.SortBy, search.Descending)
            .Include(q => q.Subject)
            .Include(q => q.Level)
            .Include(q => q.Type)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(ct);
        return new Page<Quiz>(items, total, page.PageNumber, page.PageSize);
    }

    public async Task<IReadOnlySet<int>> GetTakenAsync(IReadOnlyCollection<int> quizIds, CancellationToken ct = default)
    {
        var ids = quizIds.ToList();
        var taken = await db.Attempts
            .Where(a => a.QuizId != null && ids.Contains(a.QuizId.Value))
            .Select(a => a.QuizId!.Value)
            .Distinct()
            .ToListAsync(ct);
        return taken.ToHashSet();
    }

    public async Task<IReadOnlyList<Subject>> GetSubjectsAsync(int? expertId, CancellationToken ct = default) =>
        await db.Subjects.AsNoTracking()
            .Where(s => expertId == null || s.ExpertId == expertId)
            .OrderBy(s => s.Name).ThenBy(s => s.Id)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Setting>> GetTypesAsync(CancellationToken ct = default) =>
        await db.Settings.AsNoTracking()
            .Where(s => s.Type == SettingType.TestType)
            .OrderBy(s => s.DisplayOrder).ThenBy(s => s.Id)
            .ToListAsync(ct);

    // Id, subject and level sort the list (SRS III 7.4); the level follows its order in
    // the settings. Without a chosen heading the newest quiz comes first. The id is
    // always the last key, so rows with equal values keep their place between pages.
    private static IQueryable<Quiz> Sort(IQueryable<Quiz> query, string? sortBy, bool descending)
    {
        IOrderedQueryable<Quiz> ordered = sortBy?.ToLowerInvariant() switch
        {
            "id" => descending ? query.OrderByDescending(q => q.Id) : query.OrderBy(q => q.Id),
            "subject" => descending ? query.OrderByDescending(q => q.Subject.Name) : query.OrderBy(q => q.Subject.Name),
            "level" => descending ? query.OrderByDescending(q => q.Level.DisplayOrder) : query.OrderBy(q => q.Level.DisplayOrder),
            _ => query.OrderByDescending(q => q.Id),
        };
        return descending || sortBy is null ? ordered.ThenByDescending(q => q.Id) : ordered.ThenBy(q => q.Id);
    }
}
