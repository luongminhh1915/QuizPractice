using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizPractice.Application.Dtos.Public;
using QuizPractice.Application.Services.Public;
using QuizPractice.Domain.Constants;

namespace QuizPractice.Api.Controllers.Public;

/// <summary>
/// Subject Register (F#12; UC-14). The team limits it to a signed-in customer (OI-15), so
/// unlike the rest of the catalog it is not open: a guest gets 401, another role 403.
/// </summary>
[Route("api/subjects/{subjectId:int}/registration")]
[Authorize(Roles = Roles.Customer)]
public class SubjectRegisterController(ISubjectRegisterService register) : ApiControllerBase
{
    /// <summary>Packages, the contact from the profile, profile problems (E1) and an existing registration (A3).</summary>
    [HttpGet]
    public async Task<ActionResult<SubjectRegisterFormDto>> Form(int subjectId, CancellationToken ct) =>
        FromResult(await register.GetFormAsync(subjectId, ct));

    /// <summary>201 with the stored registration; 409 for A3 and E2, 400 Profile.Incomplete for E1.</summary>
    [HttpPost]
    public async Task<ActionResult<SubjectRegisteredDto>> Submit(int subjectId, SubjectRegisterRequest request, CancellationToken ct)
    {
        var result = await register.RegisterAsync(subjectId, request, ct);
        return result.IsSuccess ? StatusCode(StatusCodes.Status201Created, result.Value) : Problem(result.Error!);
    }
}
