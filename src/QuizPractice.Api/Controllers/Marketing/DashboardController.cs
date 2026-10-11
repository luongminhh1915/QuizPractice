using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizPractice.Application.Dtos.Marketing;
using QuizPractice.Application.Services.Marketing;
using QuizPractice.Domain.Constants;

namespace QuizPractice.Api.Controllers.Marketing;

/// <summary>
/// Management Dashboard (PDF II.5, F#19; UC-34): GET /api/dashboard?from=2026-10-05&amp;to=2026-10-11.
/// The service refuses every role but Marketing, Sale and Admin.
/// </summary>
[Route("api/dashboard")]
[Authorize(Policy = SettingValues.MenuItems.Dashboard)]
public class DashboardController(IDashboardService dashboard) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Get([FromQuery] DashboardRequest request, CancellationToken ct) =>
        FromResult(await dashboard.GetAsync(request, ct));
}
