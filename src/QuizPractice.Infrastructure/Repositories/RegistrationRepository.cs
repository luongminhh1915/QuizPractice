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

    public async Task<IReadOnlyList<Registration>> ListOwnByIdsAsync(int userId, IReadOnlyCollection<int> ids, CancellationToken ct = default) =>
        await db.Registrations
            .Include(r => r.Subject)
            .Include(r => r.Package)
            .Include(r => r.Invoice)
            .Where(r => r.UserId == userId && ids.Contains(r.Id))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Subject>> ListAccessibleSubjectsAsync(int userId, DateOnly day, CancellationToken ct = default) =>
        await db.Subjects.AsNoTracking()
            .Where(s => db.Registrations.Any(r => r.UserId == userId && r.SubjectId == s.Id
                && r.Status == RegistrationStatus.Paid && r.ValidFrom <= day && r.ValidTo >= day))
            .OrderBy(s => s.Name).ThenBy(s => s.Id)
            .ToListAsync(ct);

    public async Task<Page<Registration>> SearchAsync(RegistrationSearch search, PageRequest page, CancellationToken ct = default)
    {
        var query = db.Registrations.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search.Text))
        {
            var text = search.Text.Trim();
            query = query.Where(r => r.Email.Contains(text));
        }

        if (search.SubjectId is { } subjectId)
        {
            query = query.Where(r => r.SubjectId == subjectId);
        }

        if (search.From is { } from)
        {
            query = query.Where(r => r.RegisteredAt >= from);
        }

        if (search.Before is { } before)
        {
            query = query.Where(r => r.RegisteredAt < before);
        }

        if (search.Status is { } status)
        {
            query = query.Where(r => r.Status == status);
        }

        var total = await query.CountAsync(ct);
        var items = await Sort(query, search.SortBy, search.Descending)
            .Include(r => r.Subject)
            .Include(r => r.Package)
            .Include(r => r.UpdatedBy)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(ct);
        return new Page<Registration>(items, total, page.PageNumber, page.PageSize);
    }

    // The nine columns of SRS III 8.1; the newest registration comes first by default. The
    // id is always the last key, so rows with equal values keep their place between pages.
    private static IQueryable<Registration> Sort(IQueryable<Registration> query, string? sortBy, bool descending)
    {
        IOrderedQueryable<Registration> ordered = sortBy?.ToLowerInvariant() switch
        {
            "id" => descending ? query.OrderByDescending(r => r.Id) : query.OrderBy(r => r.Id),
            "email" => descending ? query.OrderByDescending(r => r.Email) : query.OrderBy(r => r.Email),
            "subject" => descending ? query.OrderByDescending(r => r.Subject.Name) : query.OrderBy(r => r.Subject.Name),
            "package" => descending ? query.OrderByDescending(r => r.Package.Name) : query.OrderBy(r => r.Package.Name),
            "totalcost" => descending ? query.OrderByDescending(r => r.TotalCost) : query.OrderBy(r => r.TotalCost),
            "status" => descending ? query.OrderByDescending(r => r.Status) : query.OrderBy(r => r.Status),
            "validfrom" => descending ? query.OrderByDescending(r => r.ValidFrom) : query.OrderBy(r => r.ValidFrom),
            "validto" => descending ? query.OrderByDescending(r => r.ValidTo) : query.OrderBy(r => r.ValidTo),
            "registeredat" => descending ? query.OrderByDescending(r => r.RegisteredAt) : query.OrderBy(r => r.RegisteredAt),
            _ => query.OrderByDescending(r => r.RegisteredAt),
        };
        return descending || sortBy is null ? ordered.ThenByDescending(r => r.Id) : ordered.ThenBy(r => r.Id);
    }

    public Task<Registration?> GetForSaleAsync(int id, CancellationToken ct = default) =>
        db.Registrations
            .Include(r => r.Subject).ThenInclude(s => s.PricePackages)
            .Include(r => r.Package)
            .Include(r => r.Invoice)
            .Include(r => r.User)
            .Include(r => r.CreatedBy)
            .Include(r => r.UpdatedBy)
            .AsSplitQuery()
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<Subject>> ListOfferedSubjectsAsync(CancellationToken ct = default) =>
        await db.Subjects.AsNoTracking()
            .Include(s => s.PricePackages)
            .Where(s => s.Status == SubjectStatus.Published && s.PricePackages.Any(p => p.Status == ActiveStatus.Active))
            .OrderBy(s => s.Name).ThenBy(s => s.Id)
            .ToListAsync(ct);

    public void Add(Registration registration) => db.Registrations.Add(registration);
}
