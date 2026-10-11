using QuizPractice.Domain.Entities;

namespace QuizPractice.Application.Dtos.Public;

/// <summary>A shown post as a card or a sider line (SRS III 2.1 to 2.3).</summary>
public sealed record PostCardDto(int Id, string Title, string Thumbnail, string Brief, string Category, DateTime UpdatedAt)
{
    public static PostCardDto From(Post p) =>
        new(p.Id, p.Title, p.Thumbnail, p.Brief, p.Category.Value, DateTime.SpecifyKind(p.UpdatedAt, DateTimeKind.Utc));
}

/// <summary>
/// The prices a subject is shown with: those of its active package with the lowest
/// sale price (UC-12 step 3, UC-13 step 3).
/// </summary>
public sealed record PriceDto(int PackageId, string PackageName, int AccessMonths, decimal ListPrice, decimal SalePrice)
{
    public static PriceDto From(PricePackage p) => new(p.Id, p.Name, p.AccessMonths, p.ListPrice, p.SalePrice);

    /// <summary>Null when the subject has no active package (UC-12 E2): no price and no Register.</summary>
    public static PriceDto? LowestOf(Subject s) =>
        s.PricePackages.Where(p => p.Status == Domain.Enums.ActiveStatus.Active)
            .OrderBy(p => p.SalePrice).ThenBy(p => p.ListPrice).ThenBy(p => p.Id)
            .Select(From)
            .FirstOrDefault();
}

/// <summary>A published subject as a card or a sider line (SRS III 2.1, 2.4).</summary>
public sealed record SubjectCardDto(int Id, string Name, string Thumbnail, string? Tagline, string Category, PriceDto? LowestPrice)
{
    public static SubjectCardDto From(Subject s) =>
        new(s.Id, s.Name, s.Thumbnail, s.Tagline, s.Category.Value, PriceDto.LowestOf(s));
}

/// <summary>A post or subject category of the system settings, as the sider lists it (BR-16).</summary>
public sealed record CategoryDto(int Id, string Name)
{
    public static CategoryDto From(Setting s) => new(s.Id, s.Value);
}

/// <summary>Criteria of Blogs List (UC-10). Ten posts a page, as every list (SRS III 0.3).</summary>
public sealed record BlogListRequest(string? Search = null, int? CategoryId = null, int Page = 1, int PageSize = 10);

/// <summary>The sider of Blogs List and Blog Details: active post categories and the latest posts.</summary>
public sealed record BlogSiderDto(IReadOnlyList<CategoryDto> Categories, IReadOnlyList<PostCardDto> LatestPosts);

/// <summary>
/// Blog Details (UC-11): the post as a reader sees it. Content is the HTML stored by Post
/// Details, already cleaned by the sanitizer when it was saved (UC-36).
/// </summary>
public sealed record BlogPostDto(
    int Id,
    string Title,
    string Thumbnail,
    string Brief,
    string Content,
    int CategoryId,
    string Category,
    string Author,
    DateTime UpdatedAt)
{
    public static BlogPostDto From(Post p) =>
        new(p.Id, p.Title, p.Thumbnail, p.Brief, p.Content, p.CategoryId, p.Category.Value, p.Author.FullName,
            DateTime.SpecifyKind(p.UpdatedAt, DateTimeKind.Utc));
}

/// <summary>Criteria of Subjects List (UC-12). Nine a page: three rows of three cards.</summary>
public sealed record SubjectListRequest(string? Search = null, int? CategoryId = null, int Page = 1, int PageSize = 9);

/// <summary>The sider of Subjects List and Subject Details: active subject categories and the featured subjects.</summary>
public sealed record SubjectSiderDto(IReadOnlyList<CategoryDto> Categories, IReadOnlyList<SubjectCardDto> FeaturedSubjects);

/// <summary>Home Page (UC-09): the blocks besides the sliders, which have their own endpoint.</summary>
public sealed record HomeDto(
    IReadOnlyList<PostCardDto> HotPosts,
    IReadOnlyList<SubjectCardDto> FeaturedSubjects,
    IReadOnlyList<PostCardDto> LatestPosts);
