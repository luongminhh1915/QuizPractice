using QuizPractice.Application.Dtos.Common;

namespace QuizPractice.Application.Services.Common;

/// <summary>The account of the caller: session, sign-in, register, links, profile (Common, UC-01 to UC-08).</summary>
public interface IAccountService
{
    /// <summary>The signed-in user, or null for a Guest.</summary>
    Task<CurrentUserDto?> GetCurrentAsync(CancellationToken ct = default);

    /// <summary>
    /// Whether a session opened earlier may go on: the account is still Active and still
    /// holds the role written in the cookie. Checked on every request, so an account the
    /// Admin deactivates or moves to another role is signed out at once.
    /// </summary>
    Task<bool> IsSessionValidAsync(int userId, string role, CancellationToken ct = default);
}
