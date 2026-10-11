using QuizPractice.Application.Common;
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

    /// <summary>UC-01: checks the credentials and the account status; the Api then writes the cookie.</summary>
    Task<Result<SignedInUser>> LoginAsync(LoginRequest request, CancellationToken ct = default);

    /// <summary>
    /// UC-02: stores a Customer account as Unverified and mails the verification link.
    /// E3: when the mail fails the account stays and the error offers a new link.
    /// </summary>
    Task<Result<RegisteredDto>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);

    /// <summary>
    /// UC-01 E2, UC-02 E3, UC-03 E1: a new verification link for an account still
    /// Unverified. Succeeds for any email, so it never tells which emails have an account.
    /// </summary>
    Task<Result> SendVerificationLinkAsync(SendVerificationLinkRequest request, CancellationToken ct = default);
}
