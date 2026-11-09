using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuizPractice.Domain.Constants;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Infrastructure.Persistence.Seed;

/// <summary>
/// Adds the System Menu items a database seeded before them does not have yet, after the
/// last item, so a menu item added in code (My Subjects, UC-71) reaches every team
/// database without dropping it. An item the Admin has turned off stays off: only a
/// missing value is written.
/// </summary>
internal static class DevMenuSeeder
{
    public static async Task SeedAsync(AppDbContext db, ILogger logger, CancellationToken ct = default)
    {
        var present = await db.Settings.Where(s => s.Type == SettingType.SystemMenu).Select(s => s.Value).ToListAsync(ct);
        var missing = SettingValues.MenuItems.All.Where(item => !present.Contains(item)).ToList();
        if (missing.Count == 0)
        {
            return;
        }

        var order = (await db.Settings.Where(s => s.Type == SettingType.SystemMenu).MaxAsync(s => (int?)s.DisplayOrder, ct) ?? 0) + 1;
        foreach (var item in missing)
        {
            db.Settings.Add(new Setting
            {
                Type = SettingType.SystemMenu,
                Value = item,
                DisplayOrder = order++,
                Description = $"Menu item that opens {item}.",
                Status = ActiveStatus.Active,
            });
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("System Menu items added: {Items}.", string.Join(", ", missing));
    }
}
