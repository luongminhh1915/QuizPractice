using Microsoft.EntityFrameworkCore;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;
using QuizPractice.Infrastructure.Persistence;

namespace QuizPractice.Infrastructure.Repositories;

public class SettingRepository(AppDbContext db) : ISettingRepository
{
    public async Task<IReadOnlyList<Setting>> ListActiveAsync(SettingType type, CancellationToken ct = default) =>
        await db.Settings.AsNoTracking()
            .Where(s => s.Type == type && s.Status == ActiveStatus.Active)
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.Id)
            .ToListAsync(ct);

    public Task<Setting?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.Settings.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<Page<Setting>> SearchAsync(SettingSearch search, PageRequest page, CancellationToken ct = default)
    {
        var query = db.Settings.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search.Text))
        {
            var text = search.Text.Trim();
            query = query.Where(s => s.Value.Contains(text));
        }

        if (search.Type is { } type)
        {
            query = query.Where(s => s.Type == type);
        }

        if (search.Status is { } status)
        {
            query = query.Where(s => s.Status == status);
        }

        var total = await query.CountAsync(ct);
        var items = await Sort(query, search.SortBy, search.Descending)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(ct);
        return new Page<Setting>(items, total, page.PageNumber, page.PageSize);
    }

    // The column collation of SQL Server ignores case, so "easy" matches "Easy".
    public Task<bool> ExistsAsync(SettingType type, string value, int? exceptId, CancellationToken ct = default) =>
        db.Settings.AnyAsync(s => s.Type == type && s.Value == value && s.Id != exceptId, ct);

    public async Task<IReadOnlyList<SettingUse>> GetUseAsync(Setting setting, CancellationToken ct = default)
    {
        var id = setting.Id;
        var uses = setting.Type switch
        {
            SettingType.UserRole => [new("accounts", await db.Users.CountAsync(u => u.RoleId == id, ct))],
            SettingType.PostCategory => [new("posts", await db.Posts.CountAsync(p => p.CategoryId == id, ct))],
            SettingType.SubjectCategory => [new("subjects", await db.Subjects.CountAsync(s => s.CategoryId == id, ct))],
            SettingType.TestType => [new("quizzes", await db.Quizzes.CountAsync(q => q.TypeId == id, ct))],
            SettingType.QuestionLevel => new List<SettingUse>
            {
                new("questions", await db.Questions.CountAsync(q => q.LevelId == id, ct)),
                new("quizzes", await db.Quizzes.CountAsync(q => q.LevelId == id, ct)),
            },
            SettingType.LessonType => [new("lessons", await db.Lessons.CountAsync(l => l.TypeId == id, ct))],
            SettingType.DimensionType => new List<SettingUse>
            {
                new("subject dimensions", await db.SubjectDimensions.CountAsync(d => d.TypeId == id, ct)),
                new("quizzes drawn by it", await db.Quizzes.CountAsync(q => q.SelectionType == setting.Value, ct)),
            },
            _ => new List<SettingUse>(),
        };
        return uses.Where(u => u.Count > 0).ToList();
    }

    public async Task<int> NextOrderAsync(SettingType type, CancellationToken ct = default) =>
        (await db.Settings.Where(s => s.Type == type).MaxAsync(s => (int?)s.DisplayOrder, ct) ?? 0) + 1;

    public void Add(Setting setting) => db.Settings.Add(setting);

    private static IQueryable<Setting> Sort(IQueryable<Setting> query, string? sortBy, bool descending)
    {
        IOrderedQueryable<Setting> ordered = sortBy?.ToLowerInvariant() switch
        {
            "id" => descending ? query.OrderByDescending(s => s.Id) : query.OrderBy(s => s.Id),
            "type" => descending ? query.OrderByDescending(s => s.Type) : query.OrderBy(s => s.Type),
            "value" => descending ? query.OrderByDescending(s => s.Value) : query.OrderBy(s => s.Value),
            "order" => descending ? query.OrderByDescending(s => s.DisplayOrder) : query.OrderBy(s => s.DisplayOrder),
            "status" => descending ? query.OrderByDescending(s => s.Status) : query.OrderBy(s => s.Status),
            // Each type together, in the order its screens offer the values (BR-16).
            _ => query.OrderBy(s => s.Type).ThenBy(s => s.DisplayOrder),
        };
        return descending ? ordered.ThenByDescending(s => s.Id) : ordered.ThenBy(s => s.Id);
    }
}
