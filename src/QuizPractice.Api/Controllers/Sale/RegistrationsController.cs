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

    /// <summary>UC-60: the subjects on sale and their active packages.</summary>
    [HttpGet("choices")]
    public async Task<ActionResult<RegistrationChoicesDto>> Choices(CancellationToken ct) => FromResult(await registrations.GetChoicesAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RegistrationDetailsDto>> Get(int id, CancellationToken ct) => FromResult(await registrations.GetAsync(id, ct));

    /// <summary>UC-60: 201; 400 Email for a staff account, 409 Package.NotAvailable (E2).</summary>
    [HttpPost]
    public async Task<ActionResult<RegistrationDetailsDto>> Create(SaveRegistrationRequest request, CancellationToken ct)
    {
        var result = await registrations.CreateAsync(request, ct);
        return result.IsSuccess ? StatusCode(StatusCodes.Status201Created, result.Value) : Problem(result.Error!);
    }

    /// <summary>UC-60: 409 Registration.NotEditable (BR-22) or Registration.Changed (E3).</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<RegistrationDetailsDto>> Update(int id, SaveRegistrationRequest request, CancellationToken ct) =>
        FromResult(await registrations.UpdateAsync(id, request, ct));

    /// <summary>BR-22: the notes alone, at any status.</summary>
    [HttpPut("{id:int}/notes")]
    public async Task<ActionResult<RegistrationDetailsDto>> SaveNotes(int id, SaveNotesRequest request, CancellationToken ct) =>
        FromResult(await registrations.SaveNotesAsync(id, request, ct));
}
