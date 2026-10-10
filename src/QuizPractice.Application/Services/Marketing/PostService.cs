using FluentValidation;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Marketing;
using QuizPractice.Application.Validators;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.Application.Services.Marketing;

public class PostService(
    IPostRepository posts,
    IValidator<PostListRequest> listValidator) : IPostService
{
    public async Task<Result<Page<PostSummaryDto>>> ListAsync(PostListRequest request, CancellationToken ct = default)
    {
        if (await listValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        var search = new PostSearch(request.Search, request.CategoryId, request.Status, request.SortBy, request.Descending);
        var page = await posts.SearchAsync(search, new PageRequest(request.Page, request.PageSize), ct);
        return page.Map(PostSummaryDto.From);
    }

    public async Task<Result<IReadOnlyList<PostCategoryDto>>> GetCategoriesAsync(CancellationToken ct = default) =>
        (await posts.GetCategoriesAsync(ct)).Select(PostCategoryDto.From).ToList();
}
