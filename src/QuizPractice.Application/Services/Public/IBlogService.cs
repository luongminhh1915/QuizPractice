using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Public;
using QuizPractice.Domain.Common;

namespace QuizPractice.Application.Services.Public;

/// <summary>UC-10 View Blogs List, UC-11 View Blog Details: shown posts only.</summary>
public interface IBlogService
{
    Task<Result<Page<PostCardDto>>> ListAsync(BlogListRequest request, CancellationToken ct = default);

    /// <summary>UC-11. E1: 404 when the post does not exist or has been hidden.</summary>
    Task<Result<BlogPostDto>> GetAsync(int id, CancellationToken ct = default);

    /// <summary>The sider both blog pages share.</summary>
    Task<BlogSiderDto> GetSiderAsync(CancellationToken ct = default);
}
