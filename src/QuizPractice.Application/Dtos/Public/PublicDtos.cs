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

/// <summary>Home Page (UC-09): the blocks besides the sliders, which have their own endpoint.</summary>
public sealed record HomeDto(
    IReadOnlyList<PostCardDto> HotPosts,
    IReadOnlyList<SubjectCardDto> FeaturedSubjects,
    IReadOnlyList<PostCardDto> LatestPosts);
