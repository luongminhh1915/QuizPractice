using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizPractice.Api.Controllers.Marketing;
using QuizPractice.Application.Dtos.Admin;
using QuizPractice.Application.Services.Admin;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Constants;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Api.Controllers.Admin;

/// <summary>Users List and User Details (PDF II.9, F#37, F#38; UC-62, UC-63). Admin only.</summary>
[Route("api/users")]
[Authorize(Policy = SettingValues.MenuItems.Users)]
public class UsersController(IUserService users) : ApiControllerBase
{
    private const long FormLimit = FieldLength.ImageMaxBytes + 64 * 1024;

    [HttpGet]
    public async Task<ActionResult<Page<UserSummaryDto>>> List([FromQuery] UserListRequest request, CancellationToken ct) =>
        FromResult(await users.ListAsync(request, ct));

    /// <summary>The user roles of the filter and of User Details (BR-16).</summary>
    [HttpGet("choices")]
    public async Task<ActionResult<UserChoicesDto>> Choices(CancellationToken ct) =>
        FromResult(await users.GetChoicesAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDto>> Get(int id, CancellationToken ct) =>
        FromResult(await users.GetAsync(id, ct));

    /// <summary>409 User.EmailTaken when the email is registered (E1).</summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(FormLimit)]
    public async Task<ActionResult<UserSavedDto>> Create([FromForm] NewUserForm form, CancellationToken ct)
    {
        var result = await users.CreateAsync(await form.ToRequestAsync(ct), ct);
        return result.IsSuccess ? StatusCode(StatusCodes.Status201Created, result.Value) : Problem(result.Error!);
    }

    /// <summary>409 User.Self (E3) or User.OwnsSubjects (A3, resend with confirmHandOver).</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserSavedDto>> Update(int id, UpdateUserRequest request, CancellationToken ct) =>
        FromResult(await users.UpdateAsync(id, request, ct));
}

/// <summary>Transport shape of a new account in User Details (IFormFile stays in the API).</summary>
public sealed class NewUserForm
{
    public string FullName { get; set; } = string.Empty;
    public Gender Gender { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public string? Address { get; set; }
    public int RoleId { get; set; }
    public IFormFile? Avatar { get; set; }

    public async Task<CreateUserRequest> ToRequestAsync(CancellationToken ct) =>
        new(FullName, Gender, Email, Mobile, Address, RoleId, Avatar is null ? null : await FormFiles.BufferAsync(Avatar, ct));
}
