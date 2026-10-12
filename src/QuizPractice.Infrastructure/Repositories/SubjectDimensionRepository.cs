using Microsoft.EntityFrameworkCore;
using QuizPractice.Domain.Constants;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;
using QuizPractice.Infrastructure.Persistence;

namespace QuizPractice.Infrastructure.Repositories;

public class SubjectDimensionRepository(AppDbContext db) : ISubjectDimensionRepository
{
    public async Task<IReadOnlyList<SubjectDimension>> ListAsync(int subjectId, CancellationToken ct = default) =>
        await db.SubjectDimensions.AsNoTracking()
            .Include(d => d.Type)
            .Where(d => d.SubjectId == subjectId)
            .OrderBy(d => d.Type.DisplayOrder).ThenBy(d => d.Type.Value).ThenBy(d => d.Name).ThenBy(d => d.Id)
            .ToListAsync(ct);

    public Task<SubjectDimension?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.SubjectDimensions.Include(d => d.Type).FirstOrDefaultAsync(d => d.Id == id, ct);

    // The column collation of SQL Server ignores case, so "inheritance" matches "Inheritance".
    public Task<bool> ExistsAsync(int subjectId, int typeId, string name, int? exceptId, CancellationToken ct = default) =>
        db.SubjectDimensions.AnyAsync(
            d => d.SubjectId == subjectId && d.TypeId == typeId && d.Name == name && d.Id != exceptId, ct);

    public async Task<DimensionUse> GetUseAsync(int dimensionId, CancellationToken ct = default)
    {
        var questions = await db.QuestionDimensions.CountAsync(q => q.DimensionId == dimensionId, ct);
        var practices = await db.Attempts.CountAsync(
            a => a.SelectionType == SelectionType.Dimension && a.GroupId == dimensionId, ct);

        // A rule's group is a dimension only when its quiz draws by a dimension type; by
        // Topic the same number is a lesson id.
        var rules = await db.QuizRules.CountAsync(
            r => r.GroupId == dimensionId && r.Quiz.SelectionType != QuizSelection.Topic, ct);
        return new DimensionUse(questions, practices, rules);
    }

    public void Add(SubjectDimension dimension) => db.SubjectDimensions.Add(dimension);

    public void Remove(SubjectDimension dimension) => db.SubjectDimensions.Remove(dimension);
}
