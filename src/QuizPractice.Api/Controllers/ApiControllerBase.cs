using Microsoft.AspNetCore.Mvc;
using QuizPractice.Application.Common;

namespace QuizPractice.Api.Controllers;

/// <summary>
/// Base of every API controller: maps a failed Result to a ProblemDetails answer, so
/// the React api/ clients read every error the same way (SDS I.2, naming table).
/// </summary>
[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult Problem(Error error)
    {
        if (error.Type == ErrorType.Validation)
        {
            var fields = error.Fields ?? new Dictionary<string, string[]>();
            var details = new ValidationProblemDetails(fields.ToDictionary(f => f.Key, f => f.Value))
            {
                Title = error.Message,
                Status = StatusCodes.Status400BadRequest,
            };
            details.Extensions["code"] = error.Code;
            return BadRequest(details);
        }

        var status = error.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.ExternalFailure => StatusCodes.Status502BadGateway,
            _ => StatusCodes.Status500InternalServerError,
        };
        var problem = ProblemDetailsFactory.CreateProblemDetails(HttpContext, status, detail: error.Message);
        problem.Extensions["code"] = error.Code;
        return new ObjectResult(problem) { StatusCode = status };
    }

    /// <summary>200 with the value, or the problem of the error.</summary>
    protected ActionResult<T> FromResult<T>(Result<T> result) =>
        result.IsSuccess ? Ok(result.Value) : Problem(result.Error!);
}
