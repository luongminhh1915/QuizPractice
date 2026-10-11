using QuizPractice.Domain.Entities;

namespace QuizPractice.Domain.Repositories;

/// <summary>Accounts as the Common feature needs them: sign-in, register, links, profile (UC-01 to UC-08).</summary>
public interface IUserRepository
{
    /// <summary>The account with its role setting, tracked so the service can change it.</summary>
    Task<User?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>The account of a login email, with its role; the match ignores case and surrounding spaces.</summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
}
