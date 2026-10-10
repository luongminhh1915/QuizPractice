using QuizPractice.Domain.Common;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Domain.Repositories;

/// <summary>Criteria of Posts List (UC-35): text matched against the title, category and status filters.</summary>
public sealed record PostSearch(string? Text, int? CategoryId, VisibilityStatus? Status, string? SortBy, bool Descending);

public interface IPostRepository
{
    Task<Page<Post>> SearchAsync(PostSearch search, PageRequest page, CancellationToken ct = default);

    /// <summary>The Post Category settings in their display order, Inactive ones included.</summary>
    Task<IReadOnlyList<Setting>> GetCategoriesAsync(CancellationToken ct = default);
}
