using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizPractice.Application.Dtos.Sale;
using QuizPractice.Application.Services.Sale;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Constants;

namespace QuizPractice.Api.Controllers.Sale;

/// <summary>Registrations List and Registration Details (F#35, F#36; UC-59 to UC-61).</summary>
[Route("api/registrations")]
[Authorize(Policy = SettingValues.MenuItems.Registrations)]
public class RegistrationsController(IRegistrationService registrations) : ApiControllerBase
{
    /// <summary>UC-59: ?search=&amp;subjectId=&amp;from=&amp;to=&amp;status=&amp;sortBy=&amp;descending=&amp;page=. 400 To when the range ends before it starts (E1).</summary>
    [HttpGet]
    public async Task<ActionResult<Page<RegistrationSummaryDto>>> List([FromQuery] RegistrationListRequest request, CancellationToken ct) =>
        FromResult(await registrations.ListAsync(request, ct));

    /// <summary>UC-59: the subjects to filter by.</summary>
    [HttpGet("filters")]
    public async Task<ActionResult<RegistrationFiltersDto>> Filters(CancellationToken ct) => FromResult(await registrations.GetFiltersAsync(ct));
}
