using Microsoft.AspNetCore.Mvc;
using QuizPractice.Application.Dtos.Public;
using QuizPractice.Application.Services.Public;
using QuizPractice.Domain.Common;

namespace QuizPractice.Api.Controllers.Public;

/// <summary>Blogs List and Blog Details (F#8, F#9; UC-10, UC-11). Open to everyone; shown posts only.</summary>
[Route("api/public/blogs")]
public class BlogsController(IBlogService blogs) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Page<PostCardDto>>> List([FromQuery] BlogListRequest request, CancellationToken ct) =>
        FromResult(await blogs.ListAsync(request, ct));

    /// <summary>Post categories and latest posts for the sider of both blog pages.</summary>
    [HttpGet("sider")]
    public async Task<ActionResult<BlogSiderDto>> Sider(CancellationToken ct) => Ok(await blogs.GetSiderAsync(ct));
}
