using Microsoft.EntityFrameworkCore;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;
using QuizPractice.Infrastructure.Persistence;

namespace QuizPractice.Infrastructure.Repositories;

public class PostRepository(AppDbContext db) : IPostRepository
{
    public Task<Post?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.Posts
            .Include(p => p.Category)
            .Include(p => p.Author)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Page<Post>> SearchAsync(PostSearch search, PageRequest page, CancellationToken ct = default)
    {
        var query = db.Posts.AsNoTracking().Include(p => p.Category).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search.Text))
        {
            var text = search.Text.Trim();
            query = query.Where(p => p.Title.Contains(text));
        }

        if (search.CategoryId is { } categoryId)
        {
            query = query.Where(p => p.CategoryId == categoryId);
        }

        if (search.Status is { } status)
        {
            query = query.Where(p => p.Status == status);
        }

        var total = await query.CountAsync(ct);
        var items = await Sort(query, search.SortBy, search.Descending)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(ct);
        return new Page<Post>(items, total, page.PageNumber, page.PageSize);
    }

    public async Task<IReadOnlyList<Setting>> GetCategoriesAsync(CancellationToken ct = default) =>
        await db.Settings.AsNoTracking()
            .Where(s => s.Type == SettingType.PostCategory)
            .OrderBy(s => s.DisplayOrder).ThenBy(s => s.Id)
            .ToListAsync(ct);

    public Task<Setting?> GetCategoryAsync(int id, CancellationToken ct = default) =>
        db.Settings.FirstOrDefaultAsync(s => s.Id == id && s.Type == SettingType.PostCategory, ct);

    public async Task<IReadOnlyList<Post>> ListShownAsync(bool featuredOnly, int take, CancellationToken ct = default) =>
        await db.Posts.AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.Status == VisibilityStatus.Shown && (!featuredOnly || p.IsFeatured))
            .OrderByDescending(p => p.UpdatedAt).ThenByDescending(p => p.Id)
            .Take(take)
            .ToListAsync(ct);

    public Task<Post?> GetShownAsync(int id, CancellationToken ct = default) =>
        db.Posts.AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Author)
            .FirstOrDefaultAsync(p => p.Id == id && p.Status == VisibilityStatus.Shown, ct);

    public async Task<Page<Post>> SearchShownAsync(ShownPostSearch search, PageRequest page, CancellationToken ct = default)
    {
        var query = db.Posts.AsNoTracking().Include(p => p.Category).Where(p => p.Status == VisibilityStatus.Shown);

        if (!string.IsNullOrWhiteSpace(search.Text))
        {
            var text = search.Text.Trim();
            query = query.Where(p => p.Title.Contains(text));
        }

        if (search.CategoryId is { } categoryId)
        {
            query = query.Where(p => p.CategoryId == categoryId);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(p => p.UpdatedAt).ThenByDescending(p => p.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(ct);
        return new Page<Post>(items, total, page.PageNumber, page.PageSize);
    }

    public void Add(Post post) => db.Posts.Add(post);

    // Every heading of the table sorts except the thumbnail (SRS III 5.2); without a
    // chosen heading the latest change comes first. The id is always the last key, so
    // rows with equal values keep their place between pages.
    private static IQueryable<Post> Sort(IQueryable<Post> query, string? sortBy, bool descending)
    {
        IOrderedQueryable<Post> ordered = sortBy?.ToLowerInvariant() switch
        {
            "id" => descending ? query.OrderByDescending(p => p.Id) : query.OrderBy(p => p.Id),
            "title" => descending ? query.OrderByDescending(p => p.Title) : query.OrderBy(p => p.Title),
            "category" => descending ? query.OrderByDescending(p => p.Category.Value) : query.OrderBy(p => p.Category.Value),
            "featured" => descending ? query.OrderByDescending(p => p.IsFeatured) : query.OrderBy(p => p.IsFeatured),
            "status" => descending ? query.OrderByDescending(p => p.Status) : query.OrderBy(p => p.Status),
            _ => query.OrderByDescending(p => p.UpdatedAt),
        };
        return descending || sortBy is null ? ordered.ThenByDescending(p => p.Id) : ordered.ThenBy(p => p.Id);
    }
}
