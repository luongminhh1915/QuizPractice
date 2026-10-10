using QuizPractice.Domain.Common;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Domain.Repositories;

/// <summary>
/// Criteria of Questions List (UC-52). ExpertId limits the result to the subjects of that
/// Expert (BR-11); null for the Admin, who sees every subject. A question matches the
/// dimension filter when it tests at least one of DimensionIds.
/// </summary>
public sealed record QuestionSearch(
    int? ExpertId,
    string? Text,
    int? SubjectId,
    int? LessonId,
    IReadOnlyCollection<int> DimensionIds,
    int? LevelId,
    VisibilityStatus? Status,
    string? SortBy,
    bool Descending);

public interface IQuestionRepository
{
    /// <summary>The question with its subject, lesson, level and dimensions, tracked for a change.</summary>
    Task<Question?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// The question with everything Question Details shows, its options in display
    /// order, tracked for a change (UC-53).
    /// </summary>
    Task<Question?> GetWithOptionsAsync(int id, CancellationToken ct = default);

    /// <summary>Whether a quiz or a stored attempt already uses the question.</summary>
    Task<bool> IsUsedAsync(int questionId, CancellationToken ct = default);

    /// <summary>The options of the question that a stored attempt chose as its answer.</summary>
    Task<IReadOnlyList<int>> GetAnsweredOptionIdsAsync(int questionId, CancellationToken ct = default);

    /// <summary>Stages a new question with its options and dimensions; the service commits through IUnitOfWork.</summary>
    void Add(Question question);

    void RemoveOption(AnswerOption option);

    void RemoveDimension(QuestionDimension link);

    /// <summary>A page of questions with their subject, lesson, level and dimensions.</summary>
    Task<Page<Question>> SearchAsync(QuestionSearch search, PageRequest page, CancellationToken ct = default);

    /// <summary>The subjects of that Expert, or every subject when expertId is null; by name.</summary>
    Task<IReadOnlyList<Subject>> GetSubjectsAsync(int? expertId, CancellationToken ct = default);

    /// <summary>The lessons of a subject with their topic, in topic then lesson order; topics themselves are left out.</summary>
    Task<IReadOnlyList<Lesson>> GetLessonsAsync(int subjectId, CancellationToken ct = default);

    /// <summary>The dimensions of a subject with their type, by type then name.</summary>
    Task<IReadOnlyList<SubjectDimension>> GetDimensionsAsync(int subjectId, CancellationToken ct = default);

    /// <summary>The Question Level settings in their display order (BR-16).</summary>
    Task<IReadOnlyList<Setting>> GetLevelsAsync(CancellationToken ct = default);
}
