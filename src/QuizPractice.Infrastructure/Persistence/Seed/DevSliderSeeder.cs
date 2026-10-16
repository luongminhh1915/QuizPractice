using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Infrastructure.Persistence.Seed;

/// <summary>
/// Three sample sliders, so the Home Page carousel (UC-09) shows real banners and the
/// Marketing screens (UC-38 to UC-40) have rows to sort and switch. Written once: when
/// none of the sample titles is in the database yet, like the sample posts. Two are
/// Shown; the third is Hidden, to see that the Home Page leaves it out. The banners
/// (wwwroot/seed/sliders) come from scripts/e2e_code/slider_banners.py; no backlink,
/// as the sample has no real campaign page to send to.
/// </summary>
internal static class DevSliderSeeder
{
    private static readonly (string Title, string Image, bool Shown, string Notes)[] Sliders =
    [
        ("Autumn exam season: practise with simulation exams", "exam-season", true,
            "Sample slider. Points customers to the Simulation Exams of their subjects."),
        ("Java OOP and SQL Fundamentals are open for registration", "subjects-open", true,
            "Sample slider. The two subjects of the sample data."),
        ("Spring offer 2027", "spring-sale", false,
            "Sample slider, kept Hidden until the offer starts: the Home Page must not show it."),
    ];

    public static async Task SeedAsync(AppDbContext db, ILogger logger, CancellationToken ct = default)
    {
        var titles = Sliders.Select(s => s.Title).ToList();
        if (await db.Sliders.AnyAsync(s => titles.Contains(s.Title), ct))
        {
            return;
        }

        var order = (await db.Sliders.MaxAsync(s => (int?)s.DisplayOrder, ct) ?? 0) + 1;
        foreach (var s in Sliders)
        {
            db.Sliders.Add(new Slider
            {
                Title = s.Title,
                Image = $"/seed/sliders/{s.Image}.png",
                Status = s.Shown ? VisibilityStatus.Shown : VisibilityStatus.Hidden,
                DisplayOrder = order++,
                Notes = s.Notes,
            });
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Sample sliders seeded: {Count}.", Sliders.Length);
    }
}
