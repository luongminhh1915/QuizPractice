using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.TestContent;
using QuizPractice.Domain.Common;

namespace QuizPractice.Application.Services.TestContent;

/// <summary>UC-52 View Questions List, UC-53 Add or Edit Question, UC-54 Show or Hide Question.</summary>
public interface IQuestionService
{
    Task<Result<Page<QuestionSummaryDto>>> ListAsync(QuestionListRequest request, CancellationToken ct = default);

    /// <summary>The choices of the list filters; the lessons and dimensions of subjectId when given.</summary>
    Task<Result<QuestionFiltersDto>> GetFiltersAsync(int? subjectId, CancellationToken ct = default);

    Task<Result<QuestionDto>> GetAsync(int id, CancellationToken ct = default);

    Task<Result<QuestionDto>> CreateAsync(SaveQuestionRequest request, CancellationToken ct = default);

    Task<Result<QuestionDto>> UpdateAsync(int id, SaveQuestionRequest request, CancellationToken ct = default);

    /// <summary>The media file of a question, for the users who may open the question.</summary>
    Task<Result<MediaFile>> GetMediaAsync(int id, CancellationToken ct = default);

    Task<Result<QuestionSummaryDto>> ChangeStatusAsync(int id, ChangeQuestionStatusRequest request, CancellationToken ct = default);
}
