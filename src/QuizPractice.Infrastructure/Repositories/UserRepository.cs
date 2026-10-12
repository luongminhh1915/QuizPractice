using Microsoft.EntityFrameworkCore;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;
using QuizPractice.Infrastructure.Persistence;

namespace QuizPractice.Infrastructure.Repositories;

public class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByIdAsync(int id, CancellationToken ct = default) =>
        db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id, ct);

    // The column collation of SQL Server ignores case, so "An@QPS.vn" finds "an@qps.vn".
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var trimmed = email.Trim();
        return db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == trimmed, ct);
    }

    public void Add(User user) => db.Users.Add(user);

    public async Task<IReadOnlyList<User>> ListActiveByRoleAsync(string role, CancellationToken ct = default) =>
        await db.Users.AsNoTracking()
            .Where(u => u.Role.Value == role && u.Status == UserStatus.Active)
            .OrderBy(u => u.FullName).ThenBy(u => u.Id)
            .ToListAsync(ct);

    public async Task<Page<User>> SearchAsync(UserSearch search, PageRequest page, CancellationToken ct = default)
    {
        var query = db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search.Text))
        {
            var text = search.Text.Trim();
            query = query.Where(u => u.FullName.Contains(text) || u.Email.Contains(text) || u.Mobile.Contains(text));
        }

        if (search.Gender is { } gender)
        {
            query = query.Where(u => u.Gender == gender);
        }

        if (search.RoleId is { } roleId)
        {
            query = query.Where(u => u.RoleId == roleId);
        }

        if (search.Status is { } status)
        {
            query = query.Where(u => u.Status == status);
        }

        var total = await query.CountAsync(ct);
        var items = await Sort(query, search.SortBy, search.Descending)
            .Include(u => u.Role)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(ct);
        return new Page<User>(items, total, page.PageNumber, page.PageSize);
    }

    private static IQueryable<User> Sort(IQueryable<User> query, string? sortBy, bool descending)
    {
        IOrderedQueryable<User> ordered = sortBy?.ToLowerInvariant() switch
        {
            "id" => descending ? query.OrderByDescending(u => u.Id) : query.OrderBy(u => u.Id),
            "fullname" => descending ? query.OrderByDescending(u => u.FullName) : query.OrderBy(u => u.FullName),
            "gender" => descending ? query.OrderByDescending(u => u.Gender) : query.OrderBy(u => u.Gender),
            "email" => descending ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
            "mobile" => descending ? query.OrderByDescending(u => u.Mobile) : query.OrderBy(u => u.Mobile),
            "role" => descending ? query.OrderByDescending(u => u.Role.Value) : query.OrderBy(u => u.Role.Value),
            "status" => descending ? query.OrderByDescending(u => u.Status) : query.OrderBy(u => u.Status),
            _ => query.OrderByDescending(u => u.Id),
        };
        return descending || sortBy is null ? ordered.ThenByDescending(u => u.Id) : ordered.ThenBy(u => u.Id);
    }
}
