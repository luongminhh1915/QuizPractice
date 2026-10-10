using Microsoft.EntityFrameworkCore;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Repositories;
using QuizPractice.Infrastructure.Persistence;

namespace QuizPractice.Infrastructure.Repositories;

public class SliderRepository(AppDbContext db) : ISliderRepository
{
    public async Task<Page<Slider>> SearchAsync(SliderSearch search, PageRequest page, CancellationToken ct = default)
    {
        var query = db.Sliders.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search.Text))
        {
            var text = search.Text.Trim();
            query = query.Where(s => s.Title.Contains(text) || (s.Backlink != null && s.Backlink.Contains(text)));
        }

        if (search.Status is { } status)
        {
            query = query.Where(s => s.Status == status);
        }

        var total = await query.CountAsync(ct);
        var items = await Sort(query, search.SortBy, search.Descending)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(ct);
        return new Page<Slider>(items, total, page.PageNumber, page.PageSize);
    }

    // Every heading of the table sorts except the image (SRS III 5.4). The id is always
    // the last key: without a unique key, rows with equal values can move between
    // pages and show up twice or not at all.
    private static IQueryable<Slider> Sort(IQueryable<Slider> query, string? sortBy, bool descending)
    {
        IOrderedQueryable<Slider> ordered = sortBy?.ToLowerInvariant() switch
        {
            "title" => descending ? query.OrderByDescending(s => s.Title) : query.OrderBy(s => s.Title),
            "backlink" => descending ? query.OrderByDescending(s => s.Backlink) : query.OrderBy(s => s.Backlink),
            "status" => descending ? query.OrderByDescending(s => s.Status) : query.OrderBy(s => s.Status),
            "id" => descending ? query.OrderByDescending(s => s.Id) : query.OrderBy(s => s.Id),
            _ => query.OrderBy(s => s.DisplayOrder),
        };
        return descending ? ordered.ThenByDescending(s => s.Id) : ordered.ThenBy(s => s.Id);
    }
}
