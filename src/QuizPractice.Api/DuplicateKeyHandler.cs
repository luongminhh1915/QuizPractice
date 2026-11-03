using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace QuizPractice.Api;

/// <summary>
/// The services check a name or a Submitted registration before they save, but two
/// requests can pass that check together; the unique index then refuses the second.
/// That answers 409 with a reason the user can act on, instead of the 500 of an
/// unhandled error.
/// </summary>
public sealed class DuplicateKeyHandler(IProblemDetailsService problems) : IExceptionHandler
{
    // 2601: duplicate key in a unique index; 2627: in a unique constraint.
    private static bool IsDuplicateKey(Exception exception) =>
        exception is DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } };

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        if (!IsDuplicateKey(exception))
        {
            return false;
        }

        context.Response.StatusCode = StatusCodes.Status409Conflict;
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Conflict",
            Detail = "The same record was saved a moment ago, possibly from another window. Reload the page to see it.",
        };
        problem.Extensions["code"] = "Save.Duplicate";
        return await problems.TryWriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = problem });
    }
}
