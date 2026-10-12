namespace QuizPractice.Application.Abstractions;

/// <summary>
/// The mails of the account flows (Email Service, the secondary actor of UC-02 to
/// UC-04). Each returns false when the mail could not be sent, so the use case can
/// take its "mail cannot be sent" path instead of failing (UC-02 E3, UC-03 E3, UC-04 E1).
/// </summary>
public interface IEmailSender
{
    /// <summary>UC-02: the link that verifies the email (BR-01). Token comes from <see cref="ITokenService"/>.</summary>
    Task<bool> SendVerificationLinkAsync(string email, string fullName, string token, CancellationToken ct = default);

    /// <summary>UC-03: the generated login password, sent once the email is verified (OI-16).</summary>
    Task<bool> SendLoginPasswordAsync(string email, string fullName, string password, CancellationToken ct = default);

    /// <summary>UC-04: the link that sets a new password (BR-02).</summary>
    Task<bool> SendPasswordResetLinkAsync(string email, string fullName, string token, CancellationToken ct = default);

    /// <summary>
    /// UC-61 step 6 (BR-15): the account a Sale user's Paid registration created — the login
    /// link, the email, the generated password, and the notes of the Sale user.
    /// </summary>
    Task<bool> SendAccountCreatedAsync(string email, string fullName, string password, string? notes, CancellationToken ct = default);
}
