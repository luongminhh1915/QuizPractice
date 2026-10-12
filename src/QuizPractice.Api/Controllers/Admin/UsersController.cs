using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizPractice.Application.Dtos.Admin;
using QuizPractice.Application.Services.Admin;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Constants;

namespace QuizPractice.Api.Controllers.Admin;

/// <summary>Users List and User Details (PDF II.9, F#37, F#38; UC-62, UC-63). Admin only.</summary>
[Route("api/users")]
[Authorize(Policy = SettingValues.MenuItems.Users)]
public class UsersController(IUserService users) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Page<UserSummaryDto>>> List([FromQuery] UserListRequest request, CancellationToken ct) =>
        FromResult(await users.ListAsync(request, ct));

    /// <summary>The user roles of the filter and of User Details (BR-16).</summary>
    [HttpGet("choices")]
    public async Task<ActionResult<UserChoicesDto>> Choices(CancellationToken ct) =>
        FromResult(await users.GetChoicesAsync(ct));
}
