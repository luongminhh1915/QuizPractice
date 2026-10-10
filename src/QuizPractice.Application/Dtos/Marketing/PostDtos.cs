using QuizPractice.Application.Common;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Application.Dtos.Marketing;

/// <summary>Criteria of Posts List (UC-35). SortBy: id, title, category, featured or status.</summary>
public sealed record PostListRequest(
    string? Search = null,
    int? CategoryId = null,
    VisibilityStatus? Status = null,
    string? SortBy = null,
    bool Descending = false,
    int Page = 1,
    int PageSize = 10);

/// <summary>
/// Post Details form (UC-36). Thumbnail is required for a new post; on edit, leaving it
/// empty keeps the stored one. A new post starts Hidden (BR-23). On edit, Version is
/// the one the form was loaded with: a different stored version means another user
/// saved in between (E3).
/// </summary>
public sealed record SavePostRequest(
    string Title,
    int CategoryId,
    string Brief,
    string? Content,
    bool IsFeatured = false,
    VisibilityStatus Status = VisibilityStatus.Hidden,
    long? Version = null,
    FileUpload? Thumbnail = null);

/// <summary>Show or Hide Post (UC-37).</summary>
public sealed record ChangePostStatusRequest(VisibilityStatus Status);

/// <summary>A row of Posts List: no body, which can be 20,000 characters.</summary>
public sealed record PostSummaryDto(
    int Id,
    string Title,
    string Thumbnail,
    int CategoryId,
    string Category,
    bool IsFeatured,
    VisibilityStatus Status,
    DateTime UpdatedAt)
{
    public static PostSummaryDto From(Post p) =>
        new(p.Id, p.Title, p.Thumbnail, p.CategoryId, p.Category.Value, p.IsFeatured, p.Status, PostVersion.Utc(p.UpdatedAt));
}

public sealed record PostDto(
    int Id,
    string Title,
    string Thumbnail,
    int CategoryId,
    string Category,
    string Brief,
    string Content,
    bool IsFeatured,
    VisibilityStatus Status,
    int AuthorId,
    string Author,
    DateTime UpdatedAt,
    long Version)
{
    public static PostDto From(Post p) =>
        new(p.Id, p.Title, p.Thumbnail, p.CategoryId, p.Category.Value, p.Brief, p.Content, p.IsFeatured, p.Status,
            p.AuthorId, p.Author.FullName, PostVersion.Utc(p.UpdatedAt), PostVersion.Of(p));
}

/// <summary>A Post Category for the filter and the form; Inactive ones only label stored posts.</summary>
public sealed record PostCategoryDto(int Id, string Name, ActiveStatus Status)
{
    public static PostCategoryDto From(Setting s) => new(s.Id, s.Value, s.Status);
}

/// <summary>Address of an image uploaded for a post body.</summary>
public sealed record ContentImageDto(string Url);

/// <summary>
/// updated_at in whole seconds is the version of a post (UC-36 E3). Whole seconds
/// because the column is datetime2(0); a number because a date sent back through a
/// form can come back in another time zone.
/// </summary>
public static class PostVersion
{
    public static long Of(Post post) => new DateTimeOffset(Utc(post.UpdatedAt)).ToUnixTimeSeconds();

    /// <summary>EF reads datetime2 without a kind; the column holds UTC.</summary>
    public static DateTime Utc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

    /// <summary>Now, cut to the second the column keeps, so the version in memory equals the stored one.</summary>
    public static DateTime Now()
    {
        var now = DateTime.UtcNow;
        return new DateTime(now.Ticks - now.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
    }
}
