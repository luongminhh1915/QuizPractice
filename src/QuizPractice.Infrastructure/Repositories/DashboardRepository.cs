using Microsoft.EntityFrameworkCore;
using QuizPractice.Domain.Constants;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;
using QuizPractice.Infrastructure.Persistence;

namespace QuizPractice.Infrastructure.Repositories;

public class DashboardRepository(AppDbContext db) : IDashboardRepository
{
    public async Task<(int Created, int Total)> CountSubjectsAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        var created = await db.Subjects.CountAsync(s => s.CreatedAt >= fromUtc && s.CreatedAt < toUtc, ct);
        var total = await db.Subjects.CountAsync(ct);
        return (created, total);
    }

    public async Task<IReadOnlyList<RegistrationPoint>> GetRegistrationsAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default) =>
        await db.Registrations.AsNoTracking()
            .Where(r => r.RegisteredAt >= fromUtc && r.RegisteredAt < toUtc)
            .Select(r => new RegistrationPoint(r.RegisteredAt, r.Status))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<PaidRegistration>> GetPaidAsync(DateOnly from, DateOnly to, CancellationToken ct = default) =>
        await db.Registrations.AsNoTracking()
            .Where(r => r.Status == RegistrationStatus.Paid && r.ValidFrom >= from && r.ValidFrom <= to)
            .Select(r => new PaidRegistration(r.ValidFrom!.Value, r.TotalCost, r.Subject.CategoryId))
            .ToListAsync(ct);

    public Task<int> CountNewCustomersAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default) =>
        db.Users.CountAsync(u => u.Role.Value == Roles.Customer && u.CreatedAt >= fromUtc && u.CreatedAt < toUtc, ct);

    public Task<int> CountFirstTimeBuyersAsync(DateOnly from, DateOnly to, CancellationToken ct = default) =>
        db.Registrations
            .Where(r => r.Status == RegistrationStatus.Paid && r.UserId != null && r.ValidFrom != null)
            .GroupBy(r => r.UserId)
            .Select(g => g.Min(r => r.ValidFrom))
            .CountAsync(first => first >= from && first <= to, ct);

    public async Task<IReadOnlyList<Setting>> GetCategoriesAsync(CancellationToken ct = default) =>
        await db.Settings.AsNoTracking()
            .Where(s => s.Type == SettingType.SubjectCategory)
            .OrderBy(s => s.DisplayOrder).ThenBy(s => s.Id)
            .ToListAsync(ct);
}
