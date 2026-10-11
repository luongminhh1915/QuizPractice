using QuizPractice.Application.Dtos.Public;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.Application.Services.Public;

/// <summary>
/// UC-09. Only what a visitor may see: shown posts and published subjects. A block with
/// nothing in it comes back empty and the page leaves it out (A1).
/// </summary>
public class HomeService(IPostRepository posts, ISubjectRepository subjects) : IHomeService
{
    public const int HotPostCount = 4;
    public const int FeaturedSubjectCount = 6;
    public const int LatestPostCount = 5;

    public async Task<HomeDto> GetAsync(CancellationToken ct = default)
    {
        var hot = await posts.ListShownAsync(featuredOnly: true, HotPostCount, ct);
        var featured = await subjects.ListFeaturedPublishedAsync(FeaturedSubjectCount, ct);
        var latest = await posts.ListShownAsync(featuredOnly: false, LatestPostCount, ct);
        return new HomeDto(
            hot.Select(PostCardDto.From).ToList(),
            featured.Select(SubjectCardDto.From).ToList(),
            latest.Select(PostCardDto.From).ToList());
    }
}
