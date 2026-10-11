using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using QuizPractice.Api.Auth;
using QuizPractice.Application.Dtos.Common;
using QuizPractice.Application.Services.Common;

namespace QuizPractice.Api.Controllers.Common;

/// <summary>The account of the caller (Common feature, F#1 to F#6; UC-01 to UC-08).</summary>
[Route("api/account")]
public class AccountController(IAccountService accounts) : ApiControllerBase
{
    /// <summary>
    /// Who is signed in, or 204 for a Guest. The page calls it when it opens, so this is
    /// also where it receives its antiforgery token.
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserDto?>> Me(CancellationToken ct)
    {
        CookieSignIn.IssueAntiforgeryToken(HttpContext);
        var user = await accounts.GetCurrentAsync(ct);
        return user is null ? NoContent() : Ok(user);
    }

    /// <summary>UC-01: signs in and answers with the user and the menu of the role.</summary>
    [HttpPost("login")]
    [EnableRateLimiting(AccountRateLimit.Policy)]
    public async Task<ActionResult<CurrentUserDto?>> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await accounts.LoginAsync(request, ct);
        if (!result.IsSuccess)
        {
            return Problem(result.Error!);
        }

        var signedIn = result.Value;
        var principal = CookieSignIn.Principal(signedIn.Id, signedIn.Email, signedIn.Role);
        // Not persistent: the cookie ends with the browser, so a shared computer does not
        // keep the session (OI-08). Within the browser it lasts 8 hours of use.
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = false });

        // The rest of this request already runs as the new user: the antiforgery token
        // must name them, and the answer is their menu.
        HttpContext.User = principal;
        CookieSignIn.IssueAntiforgeryToken(HttpContext);
        return Ok(await accounts.GetCurrentAsync(ct));
    }

    /// <summary>UC-02: 201 with the email the verification link went to.</summary>
    [HttpPost("register")]
    [EnableRateLimiting(AccountRateLimit.Policy)]
    public async Task<ActionResult<RegisteredDto>> Register(RegisterRequest request, CancellationToken ct)
    {
        var result = await accounts.RegisterAsync(request, ct);
        return result.IsSuccess ? StatusCode(StatusCodes.Status201Created, result.Value) : Problem(result.Error!);
    }

    /// <summary>UC-03: the page posts the token of the link; 400 Link.Invalid for E1.</summary>
    [HttpPost("verify")]
    public async Task<ActionResult<VerifyEmailDto>> VerifyEmail(VerifyEmailRequest request, CancellationToken ct) =>
        FromResult(await accounts.VerifyEmailAsync(request, ct));

    /// <summary>
    /// UC-08: ends the session. A1: a session that has already expired gets the same
    /// 204, so the page returns home without an error.
    /// </summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
        CookieSignIn.IssueAntiforgeryToken(HttpContext);
        return NoContent();
    }

    /// <summary>UC-01 E2, UC-02 E3, UC-03 E1: the same 204 whether or not the email has an account.</summary>
    [HttpPost("verification-link")]
    [EnableRateLimiting(AccountRateLimit.Policy)]
    public async Task<IActionResult> SendVerificationLink(SendVerificationLinkRequest request, CancellationToken ct)
    {
        var result = await accounts.SendVerificationLinkAsync(request, ct);
        return result.IsSuccess ? NoContent() : Problem(result.Error!);
    }
}
