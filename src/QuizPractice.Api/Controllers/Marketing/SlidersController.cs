using Microsoft.AspNetCore.Mvc;
using QuizPractice.Application.Dtos.Marketing;
using QuizPractice.Application.Services.Marketing;
using QuizPractice.Domain.Common;

namespace QuizPractice.Api.Controllers.Marketing;

/// <summary>
/// Sliders List and Slider Details (F#21, F#22; UC-38 to UC-40).
/// TODO(auth): restrict to the Marketing role once cookie sign-in and the role table
/// (BR-03, D23) are in place. Until then the endpoints are open — development only.
/// </summary>
[Route("api/sliders")]
public class SlidersController(ISliderService sliders) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Page<SliderDto>>> List([FromQuery] SliderListRequest request, CancellationToken ct) =>
        FromResult(await sliders.ListAsync(request, ct));
}
