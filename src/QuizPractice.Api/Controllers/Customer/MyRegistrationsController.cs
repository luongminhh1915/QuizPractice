using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizPractice.Application.Dtos.Customer;
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
}
