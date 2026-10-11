using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Infrastructure.Persistence.Seed;

/// <summary>
/// Six sample posts of the Marketing account, so the public pages (Home, Blogs List,
/// Blog Details; UC-09 to UC-11) have something real to show. Written once: when none
/// of the sample titles is in the database yet, so a database with posts made during
/// testing gets them too. Three are featured (Hot Posts on Home); one is Hidden, to see
/// that the public pages leave it out. Dates spread over the last weeks give the
/// "most recently updated first" order something to sort.
/// </summary>
internal static class DevPostSeeder
{
    private static readonly (string Title, string Category, string Thumbnail, bool Featured, bool Shown, int DaysAgo, string Brief, string Content)[] Posts =
    [
        ("5 tips for the SQL exam", "Exam Guides", "sql-exam-tips", true, true, 1,
            "Read the question twice, draw the tables, and leave the hardest joins for last.",
            "<h2>Before the exam</h2><p>Practise with the <strong>SQL Practice Exam</strong> until every join type feels routine.</p>"
            + "<h2>During the exam</h2><ol><li>Read each question twice.</li><li>Sketch the tables and their keys.</li>"
            + "<li>Write the <code>FROM</code> and <code>JOIN</code> parts first.</li><li>Check <code>GROUP BY</code> against the selected columns.</li>"
            + "<li>Leave the hardest question for last.</li></ol>"),
        ("The four pillars of OOP", "Study Tips", "java-oop-pillars", true, true, 3,
            "Encapsulation, inheritance, polymorphism and abstraction, each in one Java example.",
            "<p>Most Java interview questions come back to four ideas.</p><ul><li><strong>Encapsulation</strong>: keep fields private and expose behaviour.</li>"
            + "<li><strong>Inheritance</strong>: <code>extends</code> reuses a parent class.</li><li><strong>Polymorphism</strong>: one call, many overriding methods.</li>"
            + "<li><strong>Abstraction</strong>: interfaces describe what, not how.</li></ul><p>Try the Java OOP practice to check each one.</p>"),
        ("A 4-week study plan", "Study Tips", "study-plan", true, true, 6,
            "One topic a week, a practice every other day and a simulation exam at the end.",
            "<h2>Week by week</h2><ol><li>Week 1: read the first topic and take a 10-question practice.</li><li>Week 2: second topic, review the questions you missed.</li>"
            + "<li>Week 3: third topic, mark the hard questions for review.</li><li>Week 4: take a simulation exam twice.</li></ol>"),
        ("How simulation exams work", "Exam Guides", "simulation-exams", false, true, 9,
            "A new set of questions on every attempt, a countdown, and the same pass rate as the real exam.",
            "<p>A simulation exam draws a <em>new set</em> of questions each time, following the rules set by the expert.</p>"
            + "<p>The clock counts down; when it reaches zero the attempt is submitted. You pass at or above the pass rate of the exam.</p>"),
        ("New subjects this autumn", "News", "new-subjects", false, true, 14,
            "Java Object-Oriented Programming and SQL Fundamentals are open for registration.",
            "<p>Two subjects are open this autumn: <strong>Java Object-Oriented Programming</strong> and <strong>SQL Fundamentals</strong>.</p>"
            + "<p>Register from the Subjects page and choose the package that fits your schedule.</p>"),
        ("From quiz to first job", "Career", "junior-developer", false, false, 20,
            "What junior developer interviews ask, and how practice questions prepare you.",
            "<p>This draft is still Hidden: the public pages must not show it.</p>"),
    ];

    public static async Task SeedAsync(AppDbContext db, ILogger logger, DateTime utcNow, CancellationToken ct = default)
    {
        var titles = Posts.Select(p => p.Title).ToList();
        if (await db.Posts.AnyAsync(p => titles.Contains(p.Title), ct))
        {
            return;
        }

        var author = await db.Users.FirstOrDefaultAsync(u => u.Email == DevSeedData.MarketingEmail, ct);
        var categories = await db.Settings.Where(s => s.Type == SettingType.PostCategory).ToListAsync(ct);
        if (author is null || Posts.Any(p => categories.All(c => c.Value != p.Category)))
        {
            logger.LogWarning("Sample posts not seeded: the Marketing account or a post category is missing.");
            return;
        }

        foreach (var p in Posts)
        {
            db.Posts.Add(new Post
            {
                Author = author,
                Category = categories.First(c => c.Value == p.Category),
                Title = p.Title,
                Thumbnail = $"/seed/posts/{p.Thumbnail}.png",
                Brief = p.Brief,
                Content = p.Content,
                IsFeatured = p.Featured,
                Status = p.Shown ? VisibilityStatus.Shown : VisibilityStatus.Hidden,
                UpdatedAt = utcNow.AddDays(-p.DaysAgo),
            });
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Sample posts seeded: {Count} posts.", Posts.Length);
    }
}
