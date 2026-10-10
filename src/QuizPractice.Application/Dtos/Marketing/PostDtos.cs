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

/// <summary>A Post Category for the filter and the form; Inactive ones only label stored posts.</summary>
public sealed record PostCategoryDto(int Id, string Name, ActiveStatus Status)
{
    public static PostCategoryDto From(Setting s) => new(s.Id, s.Value, s.Status);
}

/// <summary>Times of a post: EF reads datetime2 without a kind; the column holds UTC.</summary>
public static class PostVersion
{
    public static DateTime Utc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
