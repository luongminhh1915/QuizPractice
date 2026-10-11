using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Public;
using QuizPractice.Domain.Common;

namespace QuizPractice.Application.Services.Public;

/// <summary>UC-10 View Blogs List, UC-11 View Blog Details: shown posts only.</summary>
public interface IBlogService
{
    Task<Result<Page<PostCardDto>>> ListAsync(BlogListRequest request, CancellationToken ct = default);

    /// <summary>The sider both blog pages share.</summary>
    Task<BlogSiderDto> GetSiderAsync(CancellationToken ct = default);
}
