using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Marketing;
using QuizPractice.Domain.Common;

namespace QuizPractice.Application.Services.Marketing;

/// <summary>UC-35 View Posts List.</summary>
public interface IPostService
{
    Task<Result<Page<PostSummaryDto>>> ListAsync(PostListRequest request, CancellationToken ct = default);

    Task<Result<IReadOnlyList<PostCategoryDto>>> GetCategoriesAsync(CancellationToken ct = default);
}
