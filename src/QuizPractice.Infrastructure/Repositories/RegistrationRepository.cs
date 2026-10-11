using Microsoft.EntityFrameworkCore;
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

    public void Add(Registration registration) => db.Registrations.Add(registration);
}
