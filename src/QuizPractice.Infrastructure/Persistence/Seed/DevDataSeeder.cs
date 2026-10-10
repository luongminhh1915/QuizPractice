using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuizPractice.Application.Abstractions;

namespace QuizPractice.Infrastructure.Persistence.Seed;

/// <summary>
/// Writes <see cref="DevSeedData"/> once, into a migrated database whose Settings
/// table is empty. Program.cs calls it in Development only: the sample accounts
/// share one known password. To seed again, drop and re-create the database (README).
/// </summary>
public static class DevDataSeeder
{
    /// <returns>False when the database still has migrations to apply.</returns>
    public static async Task<bool> SeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DevDataSeeder));

        var pending = (await db.Database.GetPendingMigrationsAsync(ct)).ToList();
        if (pending.Count > 0)
        {
            logger.LogWarning(
                "Sample data not seeded: {Count} migration(s) not applied ({First}...). Run dotnet ef database update (README).",
                pending.Count, pending[0]);
            return false;
        }

        if (await db.Settings.AnyAsync(ct))
        {
            logger.LogInformation("Sample data not seeded: the Settings table already has rows.");
            return true;
        }

        var data = DevSeedData.Build(hasher, DateTime.UtcNow);
        db.Settings.AddRange(data.Settings);
        db.Users.AddRange(data.Users);
        db.Subjects.AddRange(data.Subjects);
        db.Lessons.AddRange(data.Lessons);
        db.SubjectDimensions.AddRange(data.Dimensions);
        db.Questions.AddRange(data.Questions);
        db.BankAccounts.Add(data.BankAccount);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Sample data seeded: {Settings} settings, {Users} accounts, {Subjects} subjects, {Questions} questions.",
            data.Settings.Count, data.Users.Count, data.Subjects.Count, data.Questions.Count);
        return true;
    }
}
