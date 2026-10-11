using QuizPractice.Domain.Enums;

namespace QuizPractice.Application.Dtos.Common;

/// <summary>
/// The signed-in user as the header shows it (SRS III 0.1, 0.2: account menu). Menu
/// lists the System Menu items the role may open, in display order (D23); the page
/// maps each item to its screen.
/// </summary>
public sealed record CurrentUserDto(int Id, string Email, string FullName, string Role, string? Avatar, IReadOnlyList<string> Menu);

/// <summary>UC-01 Login pop-up.</summary>
public sealed record LoginRequest(string Email, string Password);

/// <summary>Who signed in: what the Api writes into the cookie.</summary>
public sealed record SignedInUser(int Id, string Email, string Role);

/// <summary>
/// UC-02 User Register pop-up: no password field, the system generates one once the
/// email is verified (OI-16). Gender is nullable only so that a missing value is
/// refused instead of turning into Male.
/// </summary>
public sealed record RegisterRequest(string FullName, Gender? Gender, string Email, string Mobile);

/// <summary>Where the verification link went (UC-02 step 7).</summary>
public sealed record RegisteredDto(string Email);

/// <summary>UC-03: the token of the link opened from the mailbox.</summary>
public sealed record VerifyEmailRequest(string Token);

/// <summary>
/// UC-03 result. AlreadyVerified: A1, nothing changed. PasswordSent false: E3, the
/// account is verified but the mail with the password failed, so the page offers
/// Reset Password instead.
/// </summary>
public sealed record VerifyEmailDto(string Email, bool AlreadyVerified, bool PasswordSent);

/// <summary>Send the verification link again (UC-01 E2, UC-02 E3, UC-03 E1).</summary>
public sealed record SendVerificationLinkRequest(string Email);
