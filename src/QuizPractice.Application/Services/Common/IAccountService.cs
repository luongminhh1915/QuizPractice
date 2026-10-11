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
    /// UC-03: a genuine, unexpired link of an Unverified account makes it Active with a
    /// generated password, which is mailed. E1: a link that is not genuine or has expired.
    /// </summary>
    Task<Result<VerifyEmailDto>> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken ct = default);

    /// <summary>
    /// UC-04: mails a reset link when the email belongs to an Active account. Succeeds
    /// the same way for any email (step 6); fails only when the mail cannot be sent (E1).
    /// </summary>
    Task<Result> SendPasswordResetLinkAsync(PasswordResetLinkRequest request, CancellationToken ct = default);

    /// <summary>UC-05 step 2: E1 when the link is not genuine, has expired or has been used.</summary>
    Task<Result> CheckResetLinkAsync(CheckResetLinkRequest request, CancellationToken ct = default);

    /// <summary>UC-05: stores the new password, which ends every reset link of the account.</summary>
    Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);

    /// <summary>
    /// UC-01 E2, UC-02 E3, UC-03 E1: a new verification link for an account still
    /// Unverified. Succeeds for any email, so it never tells which emails have an account.
    /// </summary>
    Task<Result> SendVerificationLinkAsync(SendVerificationLinkRequest request, CancellationToken ct = default);
}
