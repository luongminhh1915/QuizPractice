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

    public void Add(Attempt attempt) => db.Attempts.Add(attempt);
}
