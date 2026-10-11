using FluentValidation;
using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Common;
using QuizPractice.Application.Validators;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.Application.Services.Common;

public class AccountService(
    IUserRepository users,
    ISettingRepository settings,
    ICurrentUser currentUser,
    IPermissionTable permissions,
    IPasswordHasher hasher,
    ITokenService tokens,
    IEmailSender mail,
    IValidator<LoginRequest> loginValidator,
    IValidator<SendVerificationLinkRequest> linkValidator) : IAccountService
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

    public async Task<Result<SignedInUser>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        if (await loginValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        var user = await users.GetByEmailAsync(request.Email, ct);
        if (user is null)
        {
            return InvalidCredentials;
        }

        // E2. A self-registered account has no password before it is verified (OI-16),
        // so there is nothing to compare: say what is missing and offer a new link.
        if (user.Status == UserStatus.Unverified)
        {
            return new Error(ErrorType.Forbidden, "Auth.Unverified",
                "This account has not been verified yet. Open the link sent to your email, or send the link again.");
        }

        if (user.PasswordHash is null || !hasher.Verify(user.PasswordHash, request.Password))
        {
            return InvalidCredentials;
        }

        // E3, only told to someone who knows the password.
        if (user.Status == UserStatus.Inactive)
        {
            return new Error(ErrorType.Forbidden, "Auth.Inactive",
                "This account is inactive. Contact the organization to have it opened again.");
        }

        return new SignedInUser(user.Id, user.Email, user.Role.Value);
    }

    public async Task<Result> SendVerificationLinkAsync(SendVerificationLinkRequest request, CancellationToken ct = default)
    {
        if (await linkValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        // The answer is the same whatever the email: a stranger learns nothing here.
        // A mail that fails is logged by the sender; asking again sends another one.
        var user = await users.GetByEmailAsync(request.Email, ct);
        if (user is { Status: UserStatus.Unverified })
        {
            await SendVerificationMailAsync(user.Id, user.Email, user.FullName, ct);
        }

        return Result.Success();
    }

    /// <summary>
    /// BR-01: the link names the account and expires after the configured hours. It
    /// needs no stamp: verifying moves the account out of Unverified, which ends every
    /// verification link of the account.
    /// </summary>
    private Task<bool> SendVerificationMailAsync(int userId, string email, string fullName, CancellationToken ct) =>
        mail.SendVerificationLinkAsync(email, fullName, tokens.Create(LinkPurpose.VerifyEmail, userId, string.Empty), ct);

    /// <summary>E1: one message whether the email or the password was wrong.</summary>
    private static Error InvalidCredentials =>
        new(ErrorType.Unauthorized, "Auth.InvalidCredentials", "The email or the password is incorrect.");
}
