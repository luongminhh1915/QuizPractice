using Microsoft.EntityFrameworkCore;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;
using QuizPractice.Infrastructure.Persistence;

namespace QuizPractice.Infrastructure.Repositories;

public class AttemptRepository(AppDbContext db) : IAttemptRepository
{
    public async Task<IReadOnlyList<int>> GetShownQuestionIdsAsync(int subjectId, CancellationToken ct = default) =>
        await db.Questions.AsNoTracking()
            .Where(q => q.SubjectId == subjectId && q.Status == VisibilityStatus.Shown)
            .Select(q => q.Id)
            .ToListAsync(ct);

    public async Task<Attempt?> GetOwnAsync(int id, int userId, CancellationToken ct = default)
    {
        var attempt = await db.Attempts
            .Include(a => a.Subject)
            .Include(a => a.Quiz)
            .Include(a => a.Questions).ThenInclude(aq => aq.Question).ThenInclude(q => q.Options)
            .Include(a => a.Questions).ThenInclude(aq => aq.Question).ThenInclude(q => q.Dimensions).ThenInclude(d => d.Dimension).ThenInclude(d => d.Type)
            .AsSplitQuery()
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, ct);
        if (attempt is not null)
        {
            attempt.Questions.Sort((x, y) => x.DisplayOrder.CompareTo(y.DisplayOrder));
            foreach (var row in attempt.Questions)
            {
                row.Question.Options.Sort((x, y) => x.DisplayOrder.CompareTo(y.DisplayOrder));
            }
        }

        return attempt;
    }

    public async Task<IReadOnlyDictionary<int, string>> GetLessonNamesAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default) =>
        await db.Lessons.AsNoTracking().Where(l => ids.Contains(l.Id)).ToDictionaryAsync(l => l.Id, l => l.Name, ct);

    public async Task<IReadOnlyDictionary<int, string>> GetDimensionNamesAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default) =>
        await db.SubjectDimensions.AsNoTracking().Where(d => ids.Contains(d.Id))
            .Select(d => new { d.Id, Name = d.Type.Value + ": " + d.Name })
            .ToDictionaryAsync(d => d.Id, d => d.Name, ct);

    public async Task<Page<Attempt>> ListByUserAsync(int userId, AttemptSearch search, PageRequest page, CancellationToken ct = default)
    {
        var query = db.Attempts.AsNoTracking().Where(a => a.UserId == userId);
        if (search.SubjectId is { } subjectId)
        {
            query = query.Where(a => a.SubjectId == subjectId);
        }

        if (search.Mode is { } mode)
        {
            query = query.Where(a => a.Mode == mode);
        }

        var total = await query.CountAsync(ct);
        var ordered = search.OldestFirst
            ? query.OrderBy(a => a.StartedAt).ThenBy(a => a.Id)
            : query.OrderByDescending(a => a.StartedAt).ThenByDescending(a => a.Id);
        var items = await ordered
            .Include(a => a.Subject)
            .Include(a => a.Quiz)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(ct);
        return new Page<Attempt>(items, total, page.PageNumber, page.PageSize);
    }

    public async Task<IReadOnlyDictionary<int, int>> GetCorrectCountsAsync(IReadOnlyCollection<int> attemptIds, CancellationToken ct = default) =>
        await db.AttemptQuestions.AsNoTracking()
            .Where(aq => attemptIds.Contains(aq.AttemptId) && aq.ChosenOption != null && aq.ChosenOption.IsCorrect)
            .GroupBy(aq => aq.AttemptId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

    public async Task<IReadOnlyList<Subject>> ListTakenSubjectsAsync(int userId, CancellationToken ct = default) =>
        await db.Subjects.AsNoTracking()
            .Where(s => db.Attempts.Any(a => a.UserId == userId && a.SubjectId == s.Id))
            .OrderBy(s => s.Name).ThenBy(s => s.Id)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Attempt>> ListOpenExamsAsync(int userId, CancellationToken ct = default) =>
        await db.Attempts
            .Include(a => a.Subject)
            .Include(a => a.Quiz)
            .Include(a => a.Questions).ThenInclude(aq => aq.Question).ThenInclude(q => q.Options)
            .AsSplitQuery()
            .Where(a => a.UserId == userId && a.Mode == AttemptMode.Exam && a.Status == AttemptStatus.InProgress)
            .ToListAsync(ct);

    public void Add(Attempt attempt) => db.Attempts.Add(attempt);
}
