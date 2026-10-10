using Microsoft.EntityFrameworkCore;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;
using QuizPractice.Infrastructure.Persistence;

namespace QuizPractice.Infrastructure.Repositories;

public class QuestionRepository(AppDbContext db) : IQuestionRepository
{
    public Task<Question?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.Questions
            .Include(q => q.Subject)
            .Include(q => q.Lesson)
            .Include(q => q.Level)
            .Include(q => q.Dimensions).ThenInclude(d => d.Dimension)
            .AsSplitQuery()
            .FirstOrDefaultAsync(q => q.Id == id, ct);

    public async Task<Page<Question>> SearchAsync(QuestionSearch search, PageRequest page, CancellationToken ct = default)
    {
        var query = db.Questions.AsNoTracking().AsQueryable();

        // BR-11: an Expert never receives a question of another subject, whatever the filters say.
        if (search.ExpertId is { } expertId)
        {
            query = query.Where(q => q.Subject.ExpertId == expertId);
        }

        if (!string.IsNullOrWhiteSpace(search.Text))
        {
            var text = search.Text.Trim();
            query = query.Where(q => q.Content.Contains(text));
        }

        if (search.SubjectId is { } subjectId)
        {
            query = query.Where(q => q.SubjectId == subjectId);
        }

        if (search.LessonId is { } lessonId)
        {
            query = query.Where(q => q.LessonId == lessonId);
        }

        if (search.DimensionIds.Count > 0)
        {
            var dimensionIds = search.DimensionIds.ToList();
            query = query.Where(q => q.Dimensions.Any(d => dimensionIds.Contains(d.DimensionId)));
        }

        if (search.LevelId is { } levelId)
        {
            query = query.Where(q => q.LevelId == levelId);
        }

        if (search.Status is { } status)
        {
            query = query.Where(q => q.Status == status);
        }

        var total = await query.CountAsync(ct);
        var items = await Sort(query, search.SortBy, search.Descending)
            .Include(q => q.Subject)
            .Include(q => q.Lesson)
            .Include(q => q.Level)
            .Include(q => q.Dimensions).ThenInclude(d => d.Dimension)
            .AsSplitQuery()
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(ct);
        return new Page<Question>(items, total, page.PageNumber, page.PageSize);
    }

    public async Task<IReadOnlyList<Subject>> GetSubjectsAsync(int? expertId, CancellationToken ct = default) =>
        await db.Subjects.AsNoTracking()
            .Where(s => expertId == null || s.ExpertId == expertId)
            .OrderBy(s => s.Name).ThenBy(s => s.Id)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Lesson>> GetLessonsAsync(int subjectId, CancellationToken ct = default) =>
        await db.Lessons.AsNoTracking()
            .Include(l => l.Topic)
            .Where(l => l.SubjectId == subjectId && l.TopicId != null)
            .OrderBy(l => l.Topic!.DisplayOrder).ThenBy(l => l.DisplayOrder).ThenBy(l => l.Id)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SubjectDimension>> GetDimensionsAsync(int subjectId, CancellationToken ct = default) =>
        await db.SubjectDimensions.AsNoTracking()
            .Include(d => d.Type)
            .Where(d => d.SubjectId == subjectId)
            .OrderBy(d => d.Type.DisplayOrder).ThenBy(d => d.Name).ThenBy(d => d.Id)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Setting>> GetLevelsAsync(CancellationToken ct = default) =>
        await db.Settings.AsNoTracking()
            .Where(s => s.Type == SettingType.QuestionLevel)
            .OrderBy(s => s.DisplayOrder).ThenBy(s => s.Id)
            .ToListAsync(ct);

    // Id, subject, level and status sort the list (SRS III 7.1); the level follows its
    // order in the settings (Easy, Medium, Hard), not the alphabet. Without a chosen
    // heading the newest question comes first. The id is always the last key, so rows
    // with equal values keep their place between pages.
    private static IQueryable<Question> Sort(IQueryable<Question> query, string? sortBy, bool descending)
    {
        IOrderedQueryable<Question> ordered = sortBy?.ToLowerInvariant() switch
        {
            "id" => descending ? query.OrderByDescending(q => q.Id) : query.OrderBy(q => q.Id),
            "subject" => descending ? query.OrderByDescending(q => q.Subject.Name) : query.OrderBy(q => q.Subject.Name),
            "level" => descending ? query.OrderByDescending(q => q.Level.DisplayOrder) : query.OrderBy(q => q.Level.DisplayOrder),
            "status" => descending ? query.OrderByDescending(q => q.Status) : query.OrderBy(q => q.Status),
            _ => query.OrderByDescending(q => q.Id),
        };
        return descending || sortBy is null ? ordered.ThenByDescending(q => q.Id) : ordered.ThenBy(q => q.Id);
    }
}
