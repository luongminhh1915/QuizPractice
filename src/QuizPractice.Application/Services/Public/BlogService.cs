using FluentValidation;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Public;
using QuizPractice.Application.Validators;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.Application.Services.Public;

public class BlogService(
    IPostRepository posts,
    ISettingRepository settings,
    IValidator<BlogListRequest> listValidator) : IBlogService
{
    public async Task<Result<Page<PostCardDto>>> ListAsync(BlogListRequest request, CancellationToken ct = default)
    {
        if (await listValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        // E1: no match is an empty page, not an error; the page keeps the criteria.
        var page = await posts.SearchShownAsync(
            new ShownPostSearch(request.Search, request.CategoryId), new PageRequest(request.Page, request.PageSize), ct);
        return page.Map(PostCardDto.From);
    }

    public async Task<BlogSiderDto> GetSiderAsync(CancellationToken ct = default)
    {
        var categories = await settings.ListActiveAsync(SettingType.PostCategory, ct);
        var latest = await posts.ListShownAsync(featuredOnly: false, HomeService.LatestPostCount, ct);
        return new BlogSiderDto(categories.Select(CategoryDto.From).ToList(), latest.Select(PostCardDto.From).ToList());
    }
}
