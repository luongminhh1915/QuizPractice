using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizPractice.Application.Dtos.Customer;
using QuizPractice.Application.Dtos.Public;
using QuizPractice.Application.Services.Customer;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Constants;

namespace QuizPractice.Api.Controllers.Customer;

/// <summary>My Registrations (F#13; UC-15 to UC-17). The customer's own registrations only.</summary>
[Route("api/my/registrations")]
[Authorize(Policy = SettingValues.MenuItems.MyRegistrations)]
public class MyRegistrationsController(IMyRegistrationService registrations) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Page<MyRegistrationDto>>> List([FromQuery] MyRegistrationListRequest request, CancellationToken ct) =>
        FromResult(await registrations.ListAsync(request, ct));

    /// <summary>UC-16: the Subject Register pop-up on the stored package.</summary>
    [HttpGet("{id:int}/form")]
    public async Task<ActionResult<SubjectRegisterFormDto>> ChangeForm(int id, CancellationToken ct) =>
        FromResult(await registrations.GetChangeFormAsync(id, ct));

    /// <summary>UC-16: 409 Registration.Settled (E1), Registration.InUnpaidInvoice (E3) or Package.NotAvailable; 400 Profile.Incomplete (E2).</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<MyRegistrationDto>> Change(int id, ChangeRegistrationRequest request, CancellationToken ct) =>
        FromResult(await registrations.ChangeAsync(id, request, ct));
}
