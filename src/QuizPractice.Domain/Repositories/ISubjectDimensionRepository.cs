using QuizPractice.Domain.Entities;

namespace QuizPractice.Domain.Repositories;

/// <summary>What still refers to a dimension: tagged questions, practices drawn by it, and quiz rules drawing from it.</summary>
public sealed record DimensionUse(int Questions, int Practices, int QuizRules)
{
    public bool Any => Questions + Practices + QuizRules > 0;
}

/// <summary>The dimensions of a subject (UC-46), which the question bank and the practices group by.</summary>
public interface ISubjectDimensionRepository
{
    /// <summary>The dimensions of a subject with their type, by type order then name.</summary>
    Task<IReadOnlyList<SubjectDimension>> ListAsync(int subjectId, CancellationToken ct = default);

    /// <summary>One dimension with its type, tracked for a change.</summary>
    Task<SubjectDimension?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>Whether another dimension of the subject already has this type and name (the name ignores case).</summary>
    Task<bool> ExistsAsync(int subjectId, int typeId, string name, int? exceptId, CancellationToken ct = default);

    Task<DimensionUse> GetUseAsync(int dimensionId, CancellationToken ct = default);

    void Add(SubjectDimension dimension);

    void Remove(SubjectDimension dimension);
}
