using FluentValidation;
using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Admin;
using QuizPractice.Application.Validators;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.Application.Services.Admin;

public class UserService(
    IUserRepository users,
    ISettingRepository settings,
    ICurrentUser currentUser,
    IValidator<UserListRequest> listValidator) : IUserService
{
    public async Task<Result<Page<UserSummaryDto>>> ListAsync(UserListRequest request, CancellationToken ct = default)
    {
        if (AdminAccess.Check(currentUser) is { } denied)
        {
            return denied;
        }

        if (await listValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        var search = new UserSearch(request.Search, request.Gender, request.RoleId, request.Status, request.SortBy, request.Descending);
        var page = await users.SearchAsync(search, new PageRequest(request.Page, request.PageSize), ct);
        return page.Map(UserSummaryDto.From);
    }

    public async Task<Result<UserChoicesDto>> GetChoicesAsync(CancellationToken ct = default)
    {
        if (AdminAccess.Check(currentUser) is { } denied)
        {
            return denied;
        }

        var roles = await settings.ListActiveAsync(SettingType.UserRole, ct);
        return new UserChoicesDto(roles.Select(r => new SettingLookupDto(r.Id, r.Value)).ToList());
    }
}
