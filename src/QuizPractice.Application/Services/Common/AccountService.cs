using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Dtos.Common;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.Application.Services.Common;

public class AccountService(
    IUserRepository users,
    ISettingRepository settings,
    ICurrentUser currentUser,
    IPermissionTable permissions) : IAccountService
{
    public async Task<CurrentUserDto?> GetCurrentAsync(CancellationToken ct = default)
    {
        if (currentUser.Id is not { } id)
        {
            return null;
        }

        var user = await users.GetByIdAsync(id, ct);
        if (user is null)
        {
            return null;
        }

        // D23: the menu is the active System Menu items ∩ the items the permission
        // table gives the role. An item the Admin turns off disappears, but turning one
        // on never opens a screen the table does not allow.
        var menu = (await settings.ListActiveAsync(SettingType.SystemMenu, ct))
            .Select(s => s.Value)
            .Where(item => permissions.Allows(user.Role.Value, item))
            .ToList();
        return new CurrentUserDto(user.Id, user.Email, user.FullName, user.Role.Value, user.Avatar, menu);
    }

    public async Task<bool> IsSessionValidAsync(int userId, string role, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(userId, ct);
        return user is { Status: UserStatus.Active } && user.Role.Value == role;
    }
}
