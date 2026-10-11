using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Dtos.Common;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.Application.Services.Common;

public class AccountService(
    IUserRepository users,
    ICurrentUser currentUser) : IAccountService
{
    public async Task<CurrentUserDto?> GetCurrentAsync(CancellationToken ct = default)
    {
        if (currentUser.Id is not { } id)
        {
            return null;
        }

        var user = await users.GetByIdAsync(id, ct);
        return user is null ? null : new CurrentUserDto(user.Id, user.Email, user.FullName, user.Role.Value, user.Avatar);
    }

    public async Task<bool> IsSessionValidAsync(int userId, string role, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(userId, ct);
        return user is { Status: UserStatus.Active } && user.Role.Value == role;
    }
}
