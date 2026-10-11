using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.TestContent;
using QuizPractice.Domain.Common;

namespace QuizPractice.Application.Services.TestContent;

/// <summary>UC-56 View Quizzes List, UC-57 Add or Edit Quiz, UC-58 Delete Quiz.</summary>
public interface IQuizService
{
    Task<Result<Page<QuizSummaryDto>>> ListAsync(QuizListRequest request, CancellationToken ct = default);

    Task<Result<QuizFiltersDto>> GetFiltersAsync(CancellationToken ct = default);

    Task<Result<QuizDto>> GetAsync(int id, CancellationToken ct = default);

    /// <summary>Subjects, levels and quiz types of the Overview tab.</summary>
    Task<Result<QuizChoicesDto>> GetChoicesAsync(CancellationToken ct = default);

    /// <summary>The ways a subject's questions can be drawn, and the groups of one way with their shown questions.</summary>
    Task<Result<QuizGroupsDto>> GetGroupsAsync(int subjectId, string? drawnBy, CancellationToken ct = default);

    /// <summary>Stores a quiz and its rules; a lesson quiz also draws its fixed set (UC-57 step 7).</summary>
    Task<Result<QuizDto>> CreateAsync(SaveQuizRequest request, CancellationToken ct = default);

    /// <summary>Changes a quiz nobody has taken; a lesson quiz draws again when its rules change.</summary>
    Task<Result<QuizDto>> UpdateAsync(int id, SaveQuizRequest request, CancellationToken ct = default);

    /// <summary>Active or Inactive, also for a taken quiz (A2, BR-23).</summary>
    Task<Result<QuizDto>> ChangeStatusAsync(int id, ChangeQuizStatusRequest request, CancellationToken ct = default);

    /// <summary>Removes a quiz nobody has taken and no lesson opens (BR-13).</summary>
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
}
