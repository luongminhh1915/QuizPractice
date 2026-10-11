using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace QuizPractice.Api.Auth;

/// <summary>
/// Refuses a POST, PUT or DELETE without a valid X-XSRF-TOKEN header (CookieSignIn).
/// MVC's AutoValidateAntiforgeryToken needs the Razor view services, which this API
/// does not load, so the same check is done here with IAntiforgery.
/// </summary>
public sealed class AntiforgeryCheck(IAntiforgery antiforgery) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var method = context.HttpContext.Request.Method;
        if (HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method))
        {
            return;
        }

        if (!await antiforgery.IsRequestValidAsync(context.HttpContext))
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Detail = "The page is out of date. Reload it and try again.",
                Extensions = { ["code"] = "Antiforgery.Invalid" },
            })
            { StatusCode = StatusCodes.Status400BadRequest };
        }
    }
}
