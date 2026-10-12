using Microsoft.EntityFrameworkCore;
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

    public void Add(Attempt attempt) => db.Attempts.Add(attempt);
}
