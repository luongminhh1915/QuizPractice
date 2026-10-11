using Microsoft.EntityFrameworkCore;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;
using QuizPractice.Infrastructure.Persistence;

namespace QuizPractice.Infrastructure.Repositories;

public class SettingRepository(AppDbContext db) : ISettingRepository
{
    public async Task<IReadOnlyList<Setting>> ListActiveAsync(SettingType type, CancellationToken ct = default) =>
        await db.Settings.AsNoTracking()
            .Where(s => s.Type == type && s.Status == ActiveStatus.Active)
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.Id)
            .ToListAsync(ct);
}
