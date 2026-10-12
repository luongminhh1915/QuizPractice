using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizPractice.Application.Dtos.Admin;
using QuizPractice.Application.Services.Admin;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Constants;

namespace QuizPractice.Api.Controllers.Admin;

/// <summary>Settings List and Setting Details (PDF II.9, F#39, F#40; UC-64 to UC-66). Admin only.</summary>
[Route("api/settings")]
[Authorize(Policy = SettingValues.MenuItems.Settings)]
public class SettingsController(ISettingService settings) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Page<SettingSummaryDto>>> List([FromQuery] SettingListRequest request, CancellationToken ct) =>
        FromResult(await settings.ListAsync(request, ct));
}
