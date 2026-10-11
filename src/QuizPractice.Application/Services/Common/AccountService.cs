using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Common;
using QuizPractice.Application.Validators;
using QuizPractice.Domain.Constants;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.Application.Services.Common;

public class AccountService(
    IUserRepository users,
    ISettingRepository settings,
    IUnitOfWork unitOfWork,
    IFileStorage files,
    ICurrentUser currentUser,
    IPermissionTable permissions,
    IPasswordHasher hasher,
    ITokenService tokens,
    IEmailSender mail,
    IValidator<LoginRequest> loginValidator,
    IValidator<RegisterRequest> registerValidator,
    IValidator<PasswordResetLinkRequest> resetLinkValidator,
    IValidator<ResetPasswordRequest> resetValidator,
    IValidator<SaveProfileRequest> profileValidator,
    IValidator<ChangePasswordRequest> passwordValidator,
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

    public async Task<Result<RegisteredDto>> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        if (await registerValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        var email = request.Email.Trim();
        if (await users.GetByEmailAsync(email, ct) is not null)
        {
            // E1: the page offers Login and Reset Password instead.
            return Error.Conflict("Account.EmailTaken",
                "This email is already registered. Log in, or reset the password if you have forgotten it.");
        }

        // Only a customer registers alone (OI-15); the other roles are added by the Admin (UC-63).
        var customerRole = (await settings.ListActiveAsync(SettingType.UserRole, ct)).FirstOrDefault(r => r.Value == Roles.Customer)
            ?? throw new InvalidOperationException($"The User Role setting '{Roles.Customer}' is missing or inactive.");

        var user = new User
        {
            Email = email,
            FullName = request.FullName.Trim(),
            Gender = request.Gender!.Value,
            Mobile = request.Mobile.Trim(),
            RoleId = customerRole.Id,
            Status = UserStatus.Unverified,
        };
        users.Add(user);
        await unitOfWork.SaveChangesAsync(ct);

        if (!await SendVerificationMailAsync(user.Id, user.Email, user.FullName, ct))
        {
            return new Error(ErrorType.ExternalFailure, "Mail.NotSent",
                "Your account was created, but the verification mail could not be sent. Send the link again in a moment.");
        }

        return new RegisteredDto(user.Email);
    }

    public async Task<Result<VerifyEmailDto>> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken ct = default)
    {
        // Step 2: signature and expiry; nothing about the link is stored (BR-01).
        var link = tokens.Read(LinkPurpose.VerifyEmail, request.Token ?? string.Empty);
        var user = link is null ? null : await users.GetByIdAsync(link.UserId, ct);
        if (user is null)
        {
            return InvalidLink("This verification link is not valid or has expired. Ask for a new one.");
        }

        // A1, step 3: the link only works while the account is Unverified.
        if (user.Status != UserStatus.Unverified)
        {
            return new VerifyEmailDto(user.Email, AlreadyVerified: true, PasswordSent: false);
        }

        // Steps 4 and 5 (OI-16): verified and given a password in one save.
        var password = PasswordGenerator.Generate();
        user.PasswordHash = hasher.Hash(password);
        user.Status = UserStatus.Active;
        await unitOfWork.SaveChangesAsync(ct);

        var sent = await mail.SendLoginPasswordAsync(user.Email, user.FullName, password, ct);
        return new VerifyEmailDto(user.Email, AlreadyVerified: false, PasswordSent: sent);
    }

    public async Task<Result> SendPasswordResetLinkAsync(PasswordResetLinkRequest request, CancellationToken ct = default)
    {
        if (await resetLinkValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        // Only an account that can sign in gets a link. An Unverified one verifies first
        // (UC-01 E2 offers a new verification link); an Inactive one stays closed.
        var user = await users.GetByEmailAsync(request.Email, ct);
        if (user is not { Status: UserStatus.Active, PasswordHash: { } hash })
        {
            return Result.Success();
        }

        // Step 4 (BR-02): the stamp ties the link to the current password, so setting a
        // new one ends this link and every other (UC-05 step 6). A1: a second request
        // gives a second link; both work until one of them is used or they expire.
        var token = tokens.Create(LinkPurpose.ResetPassword, user.Id, PasswordStamp(hash));
        if (!await mail.SendPasswordResetLinkAsync(user.Email, user.FullName, token, ct))
        {
            // E1, worded so that it does not say the email is registered.
            return new Error(ErrorType.ExternalFailure, "Mail.NotSent", "The mail could not be sent. Try again in a moment.");
        }

        return Result.Success();
    }

    public async Task<Result> CheckResetLinkAsync(CheckResetLinkRequest request, CancellationToken ct = default) =>
        await FindResetUserAsync(request.Token, ct) is null ? ResetLinkInvalid : Result.Success();

    public async Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        // The link first: with a dead link there is nothing to correct in the form (E1).
        var user = await FindResetUserAsync(request.Token, ct);
        if (user is null)
        {
            return ResetLinkInvalid;
        }

        if (await resetValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        // Step 6: the hash changes, so the stamp of every reset link no longer matches.
        user.PasswordHash = hasher.Hash(request.NewPassword);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<ProfileDto>> GetProfileAsync(CancellationToken ct = default)
    {
        var user = await SignedInUserAsync(ct);
        return user is null ? SignInRequired : ProfileOf(user);
    }

    public async Task<Result<ProfileDto>> UpdateProfileAsync(SaveProfileRequest request, CancellationToken ct = default)
    {
        var user = await SignedInUserAsync(ct);
        if (user is null)
        {
            return SignInRequired;
        }

        // E1, E2: nothing is stored and the previous avatar stays.
        if (await profileValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        string? oldAvatar = null;
        if (request.Avatar is not null)
        {
            oldAvatar = user.Avatar;
            user.Avatar = await files.SaveAsync(request.Avatar.Content, request.Avatar.Extension, AvatarFolder, ct);
        }
        else if (request.RemoveAvatar)
        {
            oldAvatar = user.Avatar;
            user.Avatar = null;
        }

        user.FullName = request.FullName.Trim();
        user.Gender = request.Gender!.Value;
        user.Mobile = request.Mobile.Trim();
        await unitOfWork.SaveChangesAsync(ct);

        // Only after the commit: if it had failed, the row would still point at the old file.
        if (oldAvatar is not null)
        {
            files.Delete(oldAvatar);
        }

        return ProfileOf(user);
    }

    public async Task<Result> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await SignedInUserAsync(ct);
        if (user is null)
        {
            return SignInRequired;
        }

        if (await passwordValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        // E1: shown under the field; the pop-up stays open.
        if (user.PasswordHash is null || !hasher.Verify(user.PasswordHash, request.CurrentPassword))
        {
            return Error.Invalid(nameof(request.CurrentPassword), "The current password is wrong.");
        }

        user.PasswordHash = hasher.Hash(request.NewPassword);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
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

    private const string AvatarFolder = "avatars";

    private async Task<User?> SignedInUserAsync(CancellationToken ct) =>
        currentUser.Id is { } id ? await users.GetByIdAsync(id, ct) : null;

    private static ProfileDto ProfileOf(User user) => new(user.Email, user.FullName, user.Gender, user.Mobile, user.Avatar);

    private static Error SignInRequired => new(ErrorType.Unauthorized, "Auth.Required", "Sign in to see your account.");

    /// <summary>
    /// UC-05 step 2: genuine and unexpired (BR-02), an account that can still sign in,
    /// and a password unchanged since the link was made. Null otherwise.
    /// </summary>
    private async Task<User?> FindResetUserAsync(string? token, CancellationToken ct)
    {
        var link = tokens.Read(LinkPurpose.ResetPassword, token ?? string.Empty);
        var user = link is null ? null : await users.GetByIdAsync(link.UserId, ct);
        return user is { Status: UserStatus.Active, PasswordHash: { } hash } && PasswordStamp(hash) == link!.Stamp ? user : null;
    }

    private static Error ResetLinkInvalid =>
        InvalidLink("This reset link has expired or has already been used. Ask for a new one.");

    /// <summary>
    /// Part of a hash of the stored password hash: enough to notice that the password
    /// changed, and the link never carries the hash itself.
    /// </summary>
    private static string PasswordStamp(string passwordHash) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(passwordHash)))[..16];

    /// <summary>A mailed link that is not genuine, is for something else, has expired or has been used.</summary>
    private static Error InvalidLink(string message) => new(ErrorType.Validation, "Link.Invalid", message);

    /// <summary>E1: one message whether the email or the password was wrong.</summary>
    private static Error InvalidCredentials =>
        new(ErrorType.Unauthorized, "Auth.InvalidCredentials", "The email or the password is incorrect.");
}
