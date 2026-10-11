using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Marketing;
using QuizPractice.Application.Services.Marketing;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Constants;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Api.Controllers.Marketing;

/// <summary>
/// Posts List and Post Details (PDF II.5, F#20; UC-35 to UC-37).
/// </summary>
[Route("api/posts")]
[Authorize(Policy = SettingValues.MenuItems.Posts)]
public class PostsController(IPostService posts) : ApiControllerBase
{
    /// <summary>The 2 MB thumbnail of BR-17 plus a body of up to 20,000 characters (UTF-8, up to 4 bytes each).</summary>
    private const long FormLimit = FieldLength.ImageMaxBytes + 4 * FieldLength.LongText + 64 * 1024;

    private const long ImageLimit = FieldLength.ImageMaxBytes + 16 * 1024;

    [HttpGet]
    public async Task<ActionResult<Page<PostSummaryDto>>> List([FromQuery] PostListRequest request, CancellationToken ct) =>
        FromResult(await posts.ListAsync(request, ct));

    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<PostCategoryDto>>> Categories(CancellationToken ct) =>
        FromResult(await posts.GetCategoriesAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PostDto>> Get(int id, CancellationToken ct) =>
        FromResult(await posts.GetAsync(id, ct));

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(FormLimit)]
    public async Task<ActionResult<PostDto>> Create([FromForm] PostForm form, CancellationToken ct)
    {
        var result = await posts.CreateAsync(await form.ToRequestAsync(ct), ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value.Id }, result.Value)
            : Problem(result.Error!);
    }

    [HttpPut("{id:int}")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(FormLimit)]
    public async Task<ActionResult<PostDto>> Update(int id, [FromForm] PostForm form, CancellationToken ct) =>
        FromResult(await posts.UpdateAsync(id, await form.ToRequestAsync(ct), ct));

    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<PostSummaryDto>> ChangeStatus(int id, ChangePostStatusRequest request, CancellationToken ct) =>
        FromResult(await posts.ChangeStatusAsync(id, request, ct));

    /// <summary>Image inserted into the post body by the editor; answers its address.</summary>
    [HttpPost("content-images")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ImageLimit)]
    public async Task<ActionResult<ContentImageDto>> UploadContentImage(IFormFile image, CancellationToken ct) =>
        FromResult(await posts.UploadContentImageAsync(await FormFiles.BufferAsync(image, ct), ct));
}

/// <summary>
/// Transport shape of the Post Details form. It exists only because IFormFile is an
/// ASP.NET type that Application must not see; the rules live on SavePostRequest.
/// </summary>
public sealed class PostForm
{
    public string Title { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string Brief { get; set; } = string.Empty;
    public string? Content { get; set; }
    public bool IsFeatured { get; set; }
    public VisibilityStatus Status { get; set; } = VisibilityStatus.Hidden;
    public long? Version { get; set; }
    public IFormFile? Thumbnail { get; set; }

    public async Task<SavePostRequest> ToRequestAsync(CancellationToken ct) =>
        new(Title, CategoryId, Brief, Content, IsFeatured, Status, Version,
            Thumbnail is null ? null : await FormFiles.BufferAsync(Thumbnail, ct));
}

internal static class FormFiles
{
    /// <summary>
    /// Buffered so the validator can read the signature and rewind. The request size
    /// limit of the action keeps this at about 2 MB.
    /// </summary>
    public static async Task<FileUpload> BufferAsync(IFormFile file, CancellationToken ct)
    {
        var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, ct);
        return new FileUpload(buffer, file.FileName, file.Length);
    }
}
