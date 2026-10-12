using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizPractice.Application.Dtos.Learning;
using QuizPractice.Application.Services.Learning;
using QuizPractice.Domain.Constants;

namespace QuizPractice.Api.Controllers.Learning;

/// <summary>
/// Quiz Handle and Quiz Review (F#17, F#18; UC-24 to UC-33). A practice and a simulation
/// exam run on the same screens, so this sits under the Customer role rather than under
/// one menu item. The customer's own attempts only: another one answers 404.
/// </summary>
[Route("api/attempts/{id:int}")]
[Authorize(Roles = Roles.Customer)]
public class AttemptsController(IQuizHandleService handle) : ApiControllerBase
{
    /// <summary>UC-24: 409 Attempt.Closed once scored; 403 Attempt.AccessEnded when the access has ended (E1).</summary>
    [HttpGet]
    public async Task<ActionResult<QuizHandleDto>> Get(int id, CancellationToken ct) => FromResult(await handle.GetAsync(id, ct));

    /// <summary>UC-24 step 3, A1: order is the number of the question in the attempt.</summary>
    [HttpPut("questions/{order:int}/answer")]
    public async Task<ActionResult<HandleProgressDto>> Answer(int id, int order, AnswerRequest request, CancellationToken ct) =>
        FromResult(await handle.AnswerAsync(id, order, request, ct));

    /// <summary>The media of a question, streamed with range support like the question bank does (UC-53).</summary>
    [HttpGet("questions/{order:int}/media")]
    public async Task<IActionResult> Media(int id, int order, CancellationToken ct)
    {
        var result = await handle.GetMediaAsync(id, order, ct);
        if (!result.IsSuccess)
        {
            return Problem(result.Error!);
        }

        Response.Headers.XContentTypeOptions = "nosniff";
        Response.Headers.CacheControl = "private, max-age=3600";
        return File(result.Value.Content, result.Value.ContentType, enableRangeProcessing: true);
    }
}
