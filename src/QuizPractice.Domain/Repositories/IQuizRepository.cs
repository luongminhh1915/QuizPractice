using QuizPractice.Domain.Common;
using QuizPractice.Domain.Entities;

namespace QuizPractice.Domain.Repositories;

/// <summary>
/// Criteria of Quizzes List (UC-56). ExpertId limits the result to the subjects of that
/// Expert (BR-11); null for the Admin. TypeId is a Test Type setting (Simulation, Lesson quiz).
/// </summary>
public sealed record QuizSearch(int? ExpertId, string? Text, int? SubjectId, int? TypeId, string? SortBy, bool Descending);

public interface IQuizRepository
{
    /// <summary>A page of quizzes with their subject, level and type.</summary>
    Task<Page<Quiz>> SearchAsync(QuizSearch search, PageRequest page, CancellationToken ct = default);

    /// <summary>Which of these quizzes already have an attempt (BR-13: shown as Taken).</summary>
    Task<IReadOnlySet<int>> GetTakenAsync(IReadOnlyCollection<int> quizIds, CancellationToken ct = default);

    /// <summary>The subjects of that Expert, or every subject when expertId is null; by name.</summary>
    Task<IReadOnlyList<Subject>> GetSubjectsAsync(int? expertId, CancellationToken ct = default);

    /// <summary>The Test Type settings in their display order (BR-16).</summary>
    Task<IReadOnlyList<Setting>> GetTypesAsync(CancellationToken ct = default);
}
