using QuizPractice.Domain.Common;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Domain.Repositories;

/// <summary>Criteria of Posts List (UC-35): text matched against the title, category and status filters.</summary>
public sealed record PostSearch(string? Text, int? CategoryId, VisibilityStatus? Status, string? SortBy, bool Descending);

/// <summary>Criteria of Blogs List (UC-10): text matched against the title, and a post category.</summary>
public sealed record ShownPostSearch(string? Text, int? CategoryId);

public interface IPostRepository
{
    /// <summary>The post with its category and author.</summary>
    Task<Post?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<Page<Post>> SearchAsync(PostSearch search, PageRequest page, CancellationToken ct = default);

    /// <summary>The Post Category settings in their display order, Inactive ones included.</summary>
    Task<IReadOnlyList<Setting>> GetCategoriesAsync(CancellationToken ct = default);

    /// <summary>A setting of type Post Category, or null when the id is another type or does not exist.</summary>
    Task<Setting?> GetCategoryAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Shown posts for the public pages, most recently updated first, with their category:
    /// all of them, or only the featured ones (Hot Posts of the Home Page, UC-09).
    /// </summary>
    Task<IReadOnlyList<Post>> ListShownAsync(bool featuredOnly, int take, CancellationToken ct = default);

    /// <summary>UC-10: a page of shown posts matching the criteria, most recently updated first.</summary>
    Task<Page<Post>> SearchShownAsync(ShownPostSearch search, PageRequest page, CancellationToken ct = default);

    /// <summary>Stages a new post; the service commits through IUnitOfWork.</summary>
    void Add(Post post);
}
