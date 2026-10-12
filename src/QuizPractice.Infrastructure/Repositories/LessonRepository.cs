using Microsoft.EntityFrameworkCore;
using QuizPractice.Domain.Constants;
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

    public Task<bool> HasLessonsUnderAsync(int topicId, CancellationToken ct = default) =>
        db.Lessons.AnyAsync(l => l.TopicId == topicId, ct);

    public async Task<int> NextOrderAsync(int subjectId, int? topicId, CancellationToken ct = default)
    {
        var siblings = topicId is { } id
            ? db.Lessons.Where(l => l.TopicId == id)
            : db.Lessons.Where(l => l.SubjectId == subjectId && l.Type.Value == SettingValues.LessonTypes.SubjectTopic);
        return (await siblings.MaxAsync(l => (int?)l.DisplayOrder, ct) ?? 0) + 1;
    }

    public async Task<IReadOnlyList<Quiz>> ListLessonQuizzesAsync(int subjectId, CancellationToken ct = default) =>
        await db.Quizzes.AsNoTracking()
            .Where(q => q.SubjectId == subjectId && q.Type.Value == SettingValues.TestTypes.LessonQuiz)
            .OrderBy(q => q.Name).ThenBy(q => q.Id)
            .ToListAsync(ct);

    public void Add(Lesson lesson) => db.Lessons.Add(lesson);
}
