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

/// <summary>Send the verification link again (UC-01 E2, UC-02 E3, UC-03 E1).</summary>
public sealed record SendVerificationLinkRequest(string Email);
