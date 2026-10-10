using Microsoft.AspNetCore.Mvc;
using QuizPractice.Application.Dtos.Marketing;
using QuizPractice.Application.Services.Marketing;
using QuizPractice.Domain.Common;

namespace QuizPractice.Api.Controllers.Marketing;

/// <summary>
/// Posts List and Post Details (PDF II.5, F#20; UC-35 to UC-37).
/// TODO(auth): restrict to the Marketing role once cookie sign-in and the role table
/// (BR-03, D23) are in place. Until then the endpoints are open — development only.
/// </summary>
[Route("api/posts")]
public class PostsController(IPostService posts) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Page<PostSummaryDto>>> List([FromQuery] PostListRequest request, CancellationToken ct) =>
        FromResult(await posts.ListAsync(request, ct));

    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<PostCategoryDto>>> Categories(CancellationToken ct) =>
        FromResult(await posts.GetCategoriesAsync(ct));
}
