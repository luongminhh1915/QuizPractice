using Microsoft.EntityFrameworkCore;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Repositories;
using QuizPractice.Infrastructure.Persistence;

namespace QuizPractice.Infrastructure.Repositories;

public class LessonRepository(AppDbContext db) : ILessonRepository
{
    public async Task<IReadOnlyList<Lesson>> ListAsync(int subjectId, CancellationToken ct = default) =>
        await db.Lessons.AsNoTracking()
            .Include(l => l.Type)
            .Where(l => l.SubjectId == subjectId)
            .OrderBy(l => l.DisplayOrder).ThenBy(l => l.Id)
            .ToListAsync(ct);

    public Task<Lesson?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.Lessons
            .Include(l => l.Type)
            .Include(l => l.Topic)
            .Include(l => l.Quiz)
            .FirstOrDefaultAsync(l => l.Id == id, ct);
}
