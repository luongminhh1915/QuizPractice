using Microsoft.EntityFrameworkCore;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Constants;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;
using QuizPractice.Infrastructure.Persistence;

namespace QuizPractice.Infrastructure.Repositories;

public class QuizRepository(AppDbContext db) : IQuizRepository
{
    public Task<Quiz?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.Quizzes.Include(q => q.Subject).FirstOrDefaultAsync(q => q.Id == id, ct);

    public Task<Quiz?> GetWithRulesAsync(int id, CancellationToken ct = default) =>
        db.Quizzes
            .Include(q => q.Subject)
            .Include(q => q.Level)
            .Include(q => q.Type)
            .Include(q => q.Rules)
            .Include(q => q.Questions)
            .AsSplitQuery()
            .FirstOrDefaultAsync(q => q.Id == id, ct);

    public async Task<IReadOnlyList<Lesson>> GetTopicsAsync(int subjectId, CancellationToken ct = default) =>
        await db.Lessons.AsNoTracking()
            .Where(l => l.SubjectId == subjectId && l.TopicId == null && l.Type.Value == SettingValues.LessonTypes.SubjectTopic)
            .OrderBy(l => l.DisplayOrder).ThenBy(l => l.Id)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SubjectDimension>> GetDimensionsAsync(int subjectId, CancellationToken ct = default) =>
        await db.SubjectDimensions.AsNoTracking()
            .Include(d => d.Type)
            .Where(d => d.SubjectId == subjectId)
            .OrderBy(d => d.Type.DisplayOrder).ThenBy(d => d.Name).ThenBy(d => d.Id)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<GroupMember>> GetTopicMembersAsync(int subjectId, CancellationToken ct = default) =>
        await db.Questions.AsNoTracking()
            .Where(q => q.SubjectId == subjectId && q.Status == VisibilityStatus.Shown && q.LessonId != null)
            .Select(q => new GroupMember(q.Lesson!.TopicId ?? q.Lesson.Id, q.Id))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<GroupMember>> GetDimensionMembersAsync(int subjectId, CancellationToken ct = default) =>
        await db.QuestionDimensions.AsNoTracking()
            .Where(x => x.Question.SubjectId == subjectId && x.Question.Status == VisibilityStatus.Shown)
            .Select(x => new GroupMember(x.DimensionId, x.QuestionId))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Setting>> GetLevelsAsync(CancellationToken ct = default) =>
        await db.Settings.AsNoTracking()
            .Where(s => s.Type == SettingType.QuestionLevel)
            .OrderBy(s => s.DisplayOrder).ThenBy(s => s.Id)
            .ToListAsync(ct);

    public void Add(Quiz quiz) => db.Quizzes.Add(quiz);

    public void RemoveRule(QuizRule rule) => db.QuizRules.Remove(rule);

    public void RemoveQuestion(QuizQuestion question) => db.QuizQuestions.Remove(question);

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

    public async Task<Page<Quiz>> SearchExamsAsync(ExamSearch search, PageRequest page, CancellationToken ct = default)
    {
        var subjectIds = search.SubjectIds.ToList();
        var query = db.Quizzes.AsNoTracking()
            .Where(q => subjectIds.Contains(q.SubjectId) && q.Status == ActiveStatus.Active
                && q.Type.Value == SettingValues.TestTypes.Simulation);

        if (!string.IsNullOrWhiteSpace(search.Text))
        {
            var text = search.Text.Trim();
            query = query.Where(q => q.Name.Contains(text));
        }

        if (search.SubjectId is { } subjectId)
        {
            query = query.Where(q => q.SubjectId == subjectId);
        }

        var total = await query.CountAsync(ct);
        IOrderedQueryable<Quiz> ordered = search.SortBy?.ToLowerInvariant() switch
        {
            "id" => search.Descending ? query.OrderByDescending(q => q.Id) : query.OrderBy(q => q.Id),
            "subject" => search.Descending ? query.OrderByDescending(q => q.Subject.Name) : query.OrderBy(q => q.Subject.Name),
            "name" => search.Descending ? query.OrderByDescending(q => q.Name) : query.OrderBy(q => q.Name),
            "level" => search.Descending ? query.OrderByDescending(q => q.Level.DisplayOrder) : query.OrderBy(q => q.Level.DisplayOrder),
            _ => query.OrderBy(q => q.Subject.Name).ThenBy(q => q.Name),
        };
        var items = await ordered.ThenBy(q => q.Id)
            .Include(q => q.Subject)
            .Include(q => q.Level)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(ct);
        return new Page<Quiz>(items, total, page.PageNumber, page.PageSize);
    }

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
