using Microsoft.Extensions.Options;
using QuizPractice.Application.Abstractions;
using QuizPractice.Domain.Constants;
using QuizPractice.Domain.Enums;
using QuizPractice.Infrastructure.Persistence;

namespace QuizPractice.Api.Auth;

/// <summary>The DevUser section of appsettings.Development.json.</summary>
public sealed class DevUserOptions
{
    public const string Section = "DevUser";

    /// <summary>One of <see cref="Roles"/>. Alone, it picks the first active account of that role.</summary>
    public string? Role { get; set; }

    /// <summary>Optional: a given account. Its role must match Role when both are set.</summary>
    public string? Email { get; set; }
}

/// <summary>
/// Development stand-in for the signed-in user, read once at start-up (restart to
/// switch role). TODO(auth): feature/kiet-common replaces it with the cookie sign-in.
/// </summary>
public sealed class DevCurrentUser(int? id, string? email, string? role) : ICurrentUser
{
    public int? Id { get; } = id;
    public string? Email { get; } = email;
    public string? Role { get; } = role;

    public static ICurrentUser Resolve(IServiceProvider services)
    {
        var options = services.GetRequiredService<IOptions<DevUserOptions>>().Value;
        var logger = services.GetRequiredService<ILogger<DevCurrentUser>>();
        var email = Blank(options.Email);
        var role = Blank(options.Role);

        if (email is null && role is null)
        {
            logger.LogWarning("No {Section} in appsettings.Development.json: every request is a Guest.", DevUserOptions.Section);
            return new DevCurrentUser(null, null, null);
        }

        if (role is not null && !Roles.All.Contains(role))
        {
            throw new InvalidOperationException(
                $"{DevUserOptions.Section}:Role '{role}' is not a role. Use one of: {string.Join(", ", Roles.All)}.");
        }

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = db.Users
            .Where(u => email != null ? u.Email == email : u.Role.Value == role && u.Status == UserStatus.Active)
            .OrderBy(u => u.Id)
            .Select(u => new { u.Id, u.Email, Role = u.Role.Value })
            .FirstOrDefault();

        if (user is null)
        {
            logger.LogWarning(
                "{Section} ({What}) matches no account; every request is a Guest. Is the sample data seeded (README)?",
                DevUserOptions.Section, email ?? role);
            return new DevCurrentUser(null, null, null);
        }

        if (role is not null && user.Role != role)
        {
            throw new InvalidOperationException(
                $"{DevUserOptions.Section}: {user.Email} is {user.Role}, not {role}. Fix Role, or clear Email to use any {role} account.");
        }

        logger.LogInformation("Development user: {Email} ({Role}, user_id {Id}). Change {Section} in appsettings.Development.json.",
            user.Email, user.Role, user.Id, DevUserOptions.Section);
        return new DevCurrentUser(user.Id, user.Email, user.Role);
    }

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
