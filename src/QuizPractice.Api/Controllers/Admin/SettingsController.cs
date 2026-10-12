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

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SettingDto>> Get(int id, CancellationToken ct) =>
        FromResult(await settings.GetAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<SettingSavedDto>> Create(CreateSettingRequest request, CancellationToken ct)
    {
        var result = await settings.CreateAsync(request, ct);
        return result.IsSuccess ? StatusCode(StatusCodes.Status201Created, result.Value) : Problem(result.Error!);
    }

    /// <summary>409 Setting.Changed (E2), Setting.Required, or Setting.InUse until confirmInUse (UC-66 E1).</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<SettingSavedDto>> Update(int id, UpdateSettingRequest request, CancellationToken ct) =>
        FromResult(await settings.UpdateAsync(id, request, ct));

    /// <summary>UC-66: Activate or Deactivate in place from Settings List.</summary>
    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<SettingSummaryDto>> ChangeStatus(int id, ChangeSettingStatusRequest request, CancellationToken ct) =>
        FromResult(await settings.ChangeStatusAsync(id, request, ct));
}
