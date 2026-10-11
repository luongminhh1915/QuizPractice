namespace QuizPractice.Application.Dtos.Common;

/// <summary>
/// The signed-in user as the header shows it (SRS III 0.1, 0.2: account menu). Menu
/// lists the System Menu items the role may open, in display order (D23); the page
/// maps each item to its screen.
/// </summary>
public sealed record CurrentUserDto(int Id, string Email, string FullName, string Role, string? Avatar, IReadOnlyList<string> Menu);
