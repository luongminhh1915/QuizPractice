namespace QuizPractice.Application.Abstractions;

/// <summary>
/// Who sends the request. A service asks this instead of taking a user id from the
/// client, e.g. the author of a new post (UC-36) or the subjects an Expert may see.
/// Until sign-in exists the Api reads a fixed development user from configuration;
/// the cookie sign-in of feature/kiet-common replaces that implementation.
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
