namespace QuizPractice.Application.Abstractions;

/// <summary>
/// Who sends the request. A service asks this instead of taking a user id from the
/// client, e.g. the author of a new post (UC-36) or the subjects an Expert may see.
/// The Api reads it from the sign-in cookie (HttpCurrentUser).
/// </summary>
public interface ICurrentUser
{
    /// <summary>user_id; empty for a Guest.</summary>
    int? Id { get; }

    string? Email { get; }

    /// <summary>One of <see cref="Domain.Constants.Roles"/>; empty for a Guest.</summary>
    string? Role { get; }

    bool IsAuthenticated => Id is not null;

    bool IsInRole(string role) => Role == role;
}
