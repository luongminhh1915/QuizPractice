using Microsoft.AspNetCore.Mvc;
using QuizPractice.Application.Dtos.Public;
using QuizPractice.Application.Services.Public;
using QuizPractice.Domain.Common;

namespace QuizPractice.Api.Controllers.Public;

/// <summary>
/// The public subject catalog (F#10, F#11; UC-12, UC-13). Open to everyone; published
/// subjects only. Named Catalog so it never clashes with the Subjects management screens.
/// </summary>
[Route("api/public/subjects")]
public class CatalogController(ISubjectCatalogService catalog) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Page<SubjectCardDto>>> List([FromQuery] SubjectListRequest request, CancellationToken ct) =>
        FromResult(await catalog.ListAsync(request, ct));

    /// <summary>Subject categories and featured subjects for the sider of both subject pages.</summary>
    [HttpGet("sider")]
    public async Task<ActionResult<SubjectSiderDto>> Sider(CancellationToken ct) => Ok(await catalog.GetSiderAsync(ct));
}
