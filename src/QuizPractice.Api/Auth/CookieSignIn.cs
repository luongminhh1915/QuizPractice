using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Services.Common;

namespace QuizPractice.Api.Auth;

/// <summary>
/// Sign-in of SDS I.1: an HttpOnly cookie, because the React build and the API share
/// one origin. No token sits in localStorage where a script could read it. Every
/// request that changes data also carries the antiforgery header, which another site
/// cannot read nor send.
/// </summary>
public static class CookieSignIn
{
    public const string CookieName = "qps.auth";

    /// <summary>Readable by the page on purpose: axios copies it into the X-XSRF-TOKEN header.</summary>
    public const string AntiforgeryCookieName = "XSRF-TOKEN";
    public const string AntiforgeryHeaderName = "X-XSRF-TOKEN";

    /// <summary>Duck 11/10: 8 hours, extended while the user keeps working.</summary>
    private static readonly TimeSpan SessionLength = TimeSpan.FromHours(8);

    public static IServiceCollection AddCookieSignIn(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = CookieName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.ExpireTimeSpan = SessionLength;
                options.SlidingExpiration = true;

                // An API answers with a status; the React page decides what to show.
                // The defaults would redirect to an MVC login page that does not exist.
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
                options.Events.OnValidatePrincipal = ValidateSessionAsync;
            });

        services.AddAntiforgery(options => options.HeaderName = AntiforgeryHeaderName);
        return services;
    }

    /// <summary>The principal written into the cookie: who, and with which role (BR-03).</summary>
    public static ClaimsPrincipal Principal(int userId, string email, string role) =>
        new(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role),
            ],
            CookieAuthenticationDefaults.AuthenticationScheme));

    /// <summary>
    /// Gives the page a new antiforgery token for the user of <paramref name="context"/>.
    /// The token names the user, so it is renewed whenever the user changes: on every
    /// visit (GET /api/account/me), after sign-in and after sign-out.
    /// </summary>
    public static void IssueAntiforgeryToken(HttpContext context)
    {
        var tokens = context.RequestServices.GetRequiredService<IAntiforgery>().GetAndStoreTokens(context);
        context.Response.Cookies.Append(AntiforgeryCookieName, tokens.RequestToken!, new CookieOptions
        {
            HttpOnly = false,
            SameSite = SameSiteMode.Strict,
            Secure = context.Request.IsHttps,
            Path = "/",
        });
    }

    private static async Task ValidateSessionAsync(CookieValidatePrincipalContext context)
    {
        var principal = context.Principal;
        var accounts = context.HttpContext.RequestServices.GetRequiredService<IAccountService>();
        if (principal is null
            || !int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            || principal.FindFirstValue(ClaimTypes.Role) is not { } role
            || !await accounts.IsSessionValidAsync(userId, role, context.HttpContext.RequestAborted))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}

/// <summary>The current user read from the sign-in cookie; a Guest when there is none.</summary>
public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? User => accessor.HttpContext?.User is { Identity.IsAuthenticated: true } user ? user : null;

    public int? Id => int.TryParse(User?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public string? Email => User?.FindFirstValue(ClaimTypes.Email);

    public string? Role => User?.FindFirstValue(ClaimTypes.Role);
}
