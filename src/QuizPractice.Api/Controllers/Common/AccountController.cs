using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    [AllowAnonymous]
    public async Task<ActionResult<CurrentUserDto?>> Me(CancellationToken ct)
    {
        CookieSignIn.IssueAntiforgeryToken(HttpContext);
        var user = await accounts.GetCurrentAsync(ct);
        return user is null ? NoContent() : Ok(user);
    }
}
