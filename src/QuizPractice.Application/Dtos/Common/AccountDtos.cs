namespace QuizPractice.Application.Dtos.Common;

/// <summary>The signed-in user as the header shows it (SRS III 0.1, 0.2: account menu).</summary>
public sealed record CurrentUserDto(int Id, string Email, string FullName, string Role, string? Avatar);
