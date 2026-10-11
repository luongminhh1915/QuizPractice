using Microsoft.AspNetCore.Mvc;
using QuizPractice.Application.Dtos.Public;
using QuizPractice.Application.Services.Public;

namespace QuizPractice.Api.Controllers.Public;

/// <summary>Home Page (F#7; UC-09). Open to everyone; the sliders come from GET /api/sliders/home.</summary>
[Route("api/public/home")]
public class HomeController(IHomeService home) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<HomeDto>> Get(CancellationToken ct) => Ok(await home.GetAsync(ct));
}
