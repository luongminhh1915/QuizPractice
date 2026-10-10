using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Marketing;
using QuizPractice.Domain.Common;

namespace QuizPractice.Application.Services.Marketing;

/// <summary>UC-35 View Posts List, UC-36 Add or Edit Post.</summary>
public interface IPostService
{
    Task<Result<Page<PostSummaryDto>>> ListAsync(PostListRequest request, CancellationToken ct = default);

    Task<Result<PostDto>> GetAsync(int id, CancellationToken ct = default);

    Task<Result<IReadOnlyList<PostCategoryDto>>> GetCategoriesAsync(CancellationToken ct = default);

    Task<Result<PostDto>> CreateAsync(SavePostRequest request, CancellationToken ct = default);

    Task<Result<PostDto>> UpdateAsync(int id, SavePostRequest request, CancellationToken ct = default);

    /// <summary>An image for the post body (SRS III 5.3: images in the text editor).</summary>
    Task<Result<ContentImageDto>> UploadContentImageAsync(FileUpload image, CancellationToken ct = default);
}
