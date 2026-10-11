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
/// Sliders List and Slider Details (F#21, F#22; UC-38 to UC-40).
/// The carousel of the Home Page has its own open endpoint (UC-09).
/// </summary>
[Route("api/sliders")]
[Authorize(Policy = SettingValues.MenuItems.Sliders)]
public class SlidersController(ISliderService sliders) : ApiControllerBase
{
    /// <summary>Upload limit of the form: the 2 MB image of BR-17 plus the text fields.</summary>
    private const long FormLimit = FieldLength.ImageMaxBytes + 64 * 1024;

    [HttpGet]
    public async Task<ActionResult<Page<SliderDto>>> List([FromQuery] SliderListRequest request, CancellationToken ct) =>
        FromResult(await sliders.ListAsync(request, ct));

    /// <summary>Home Page carousel (UC-09): shown sliders only, for guests too.</summary>
    [HttpGet("home")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<SliderDto>>> Home(CancellationToken ct) =>
        Ok(await sliders.ListShownAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SliderDto>> Get(int id, CancellationToken ct) =>
        FromResult(await sliders.GetAsync(id, ct));

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(FormLimit)]
    public async Task<ActionResult<SliderDto>> Create([FromForm] SliderForm form, CancellationToken ct)
    {
        var result = await sliders.CreateAsync(await form.ToRequestAsync(ct), ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value.Id }, result.Value)
            : Problem(result.Error!);
    }

    [HttpPut("{id:int}")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(FormLimit)]
    public async Task<ActionResult<SliderDto>> Update(int id, [FromForm] SliderForm form, CancellationToken ct) =>
        FromResult(await sliders.UpdateAsync(id, await form.ToRequestAsync(ct), ct));

    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<SliderDto>> ChangeStatus(int id, ChangeSliderStatusRequest request, CancellationToken ct) =>
        FromResult(await sliders.ChangeStatusAsync(id, request, ct));
}

/// <summary>
/// Transport shape of the Slider Details form. It exists only because IFormFile is an
/// ASP.NET type that Application must not see; the rules live on SaveSliderRequest.
/// </summary>
public sealed class SliderForm
{
    public string Title { get; set; } = string.Empty;
    public string? Backlink { get; set; }
    public VisibilityStatus Status { get; set; } = VisibilityStatus.Hidden;
    public string? Notes { get; set; }
    public IFormFile? Image { get; set; }

    public async Task<SaveSliderRequest> ToRequestAsync(CancellationToken ct)
    {
        FileUpload? image = null;
        if (Image is not null)
        {
            // Buffered so the validator can read the signature and rewind. The request
            // size limit above keeps this at about 2 MB.
            var buffer = new MemoryStream();
            await Image.CopyToAsync(buffer, ct);
            image = new FileUpload(buffer, Image.FileName, Image.Length);
        }

        return new SaveSliderRequest(Title, Backlink, Status, Notes, image);
    }
}
