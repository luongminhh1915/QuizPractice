using Microsoft.EntityFrameworkCore;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;
using QuizPractice.Infrastructure.Persistence;

namespace QuizPractice.Infrastructure.Repositories;

public class RegistrationRepository(AppDbContext db) : IRegistrationRepository
{
    public Task<Registration?> FindSubmittedAsync(int userId, int subjectId, CancellationToken ct = default) =>
        db.Registrations.AsNoTracking()
            .Include(r => r.Package)
            .Where(r => r.UserId == userId && r.SubjectId == subjectId && r.Status == RegistrationStatus.Submitted)
            .OrderByDescending(r => r.RegisteredAt)
            .FirstOrDefaultAsync(ct);

    public async Task<Page<Registration>> ListByUserAsync(int userId, PageRequest page, CancellationToken ct = default)
    {
        var query = db.Registrations.AsNoTracking().Where(r => r.UserId == userId);
        var total = await query.CountAsync(ct);
        var items = await query
            .Include(r => r.Subject)
            .Include(r => r.Package)
            .Include(r => r.Invoice)
            .OrderByDescending(r => r.RegisteredAt).ThenByDescending(r => r.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(ct);
        return new Page<Registration>(items, total, page.PageNumber, page.PageSize);
    }

    public Task<Registration?> GetOwnAsync(int id, int userId, CancellationToken ct = default) =>
        db.Registrations
            .Include(r => r.Subject).ThenInclude(s => s.PricePackages)
            .Include(r => r.Package)
            .Include(r => r.Invoice)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, ct);

    public void Add(Registration registration) => db.Registrations.Add(registration);
}
