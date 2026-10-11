using Microsoft.EntityFrameworkCore;
using QuizPractice.Domain.Entities;
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
}
