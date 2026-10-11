using QuizPractice.Application.Common;
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

/// <summary>UC-04: the registered email to send a reset link to.</summary>
public sealed record PasswordResetLinkRequest(string Email);

/// <summary>UC-05 step 2: is the reset link still good, before the form is shown.</summary>
public sealed record CheckResetLinkRequest(string Token);

/// <summary>UC-05: the new password, twice, with the token of the link.</summary>
public sealed record ResetPasswordRequest(string Token, string NewPassword, string ConfirmPassword);

/// <summary>UC-06 User Profile: what the pop-up shows. Email is read-only (BR-04).</summary>
public sealed record ProfileDto(string Email, string FullName, Gender Gender, string Mobile, string? Avatar);

/// <summary>
/// UC-06: the fields a user may change. Avatar is a new picture; RemoveAvatar clears
/// the stored one; neither keeps it as it is.
/// </summary>
public sealed record SaveProfileRequest(string FullName, Gender? Gender, string Mobile, FileUpload? Avatar = null, bool RemoveAvatar = false);

/// <summary>Send the verification link again (UC-01 E2, UC-02 E3, UC-03 E1).</summary>
public sealed record SendVerificationLinkRequest(string Email);
