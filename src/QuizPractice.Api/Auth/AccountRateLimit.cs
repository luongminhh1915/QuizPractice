using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace QuizPractice.Api.Auth;

/// <summary>
/// Limit on the endpoints a stranger can call without signing in: login, register and
/// the mailed links (Duck 11/10). Ten tries a minute per IP address is plenty for a
/// person and stops a script guessing passwords or filling mailboxes. Built into
/// ASP.NET Core. Use it with [EnableRateLimiting(AccountRateLimit.Policy)].
/// </summary>
public static class AccountRateLimit
{
    public const string Policy = "account";

    private const int PermitLimit = 10;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    public static IServiceCollection AddAccountRateLimit(this IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            options.AddPolicy(Policy, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = PermitLimit, Window = Window }));

            // Answered like every other error, so the page shows the sentence.
            options.OnRejected = async (context, ct) =>
            {
                var response = context.HttpContext.Response;
                response.StatusCode = StatusCodes.Status429TooManyRequests;
                await response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Detail = "Too many attempts. Wait a minute and try again.",
                }, ct);
            };
        });
}
