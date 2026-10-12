using FluentValidation;
using QuizPractice.Application.Abstractions;
using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Admin;
using QuizPractice.Application.Validators;
using QuizPractice.Domain.Common;
using QuizPractice.Domain.Constants;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;
using QuizPractice.Domain.Repositories;

namespace QuizPractice.Application.Services.Admin;

public class UserService(
    IUserRepository users,
    ISettingRepository settings,
    ISubjectRepository subjects,
    IUnitOfWork unitOfWork,
    IPasswordHasher hasher,
    IEmailSender mail,
    IFileStorage files,
    ICurrentUser currentUser,
    IValidator<UserListRequest> listValidator,
    IValidator<CreateUserRequest> createValidator,
    IValidator<UpdateUserRequest> updateValidator) : IUserService
{
    private const string AvatarFolder = "avatars";

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

    public async Task<Result<UserDto>> GetAsync(int id, CancellationToken ct = default)
    {
        if (AdminAccess.Check(currentUser) is { } denied)
        {
            return denied;
        }

        var user = await users.GetByIdAsync(id, ct);
        return user is null ? Error.NotFound("User", id) : UserDto.From(user);
    }

    public async Task<Result<UserSavedDto>> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        if (AdminAccess.Check(currentUser) is { } denied)
        {
            return denied;
        }

        if (await createValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        // E1: the email belongs to an account already; the screen opens that one instead.
        if (await users.GetByEmailAsync(request.Email, ct) is not null)
        {
            return Error.Conflict("User.EmailTaken", "This email is already registered. Its account opens instead.");
        }

        var role = await CheckRoleAsync(request.RoleId, keptRoleId: null, ct);
        if (role.Error is { } badRole)
        {
            return badRole;
        }

        // Steps 5 and 6: Active at once (the Admin vouches for the email) with a generated password.
        var password = PasswordGenerator.Generate();
        var user = new User
        {
            Email = request.Email.Trim(),
            FullName = request.FullName.Trim(),
            Gender = request.Gender,
            Mobile = request.Mobile.Trim(),
            Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim(),
            Role = role.Value,
            Status = UserStatus.Active,
            PasswordHash = hasher.Hash(password),
            Avatar = request.Avatar is null ? null : await files.SaveAsync(request.Avatar.Content, request.Avatar.Extension, AvatarFolder, ct),
        };
        users.Add(user);
        await unitOfWork.SaveChangesAsync(ct);

        // E2: a failed delivery keeps the account; the screen says so (Forgot Password sends a link).
        var sent = await mail.SendAccountByAdminAsync(user.Email, user.FullName, password, ct);
        return new UserSavedDto(UserDto.From(user), sent, []);
    }

    public async Task<Result<UserSavedDto>> UpdateAsync(int id, UpdateUserRequest request, CancellationToken ct = default)
    {
        if (AdminAccess.Check(currentUser) is { } denied)
        {
            return denied;
        }

        if (await updateValidator.CheckAsync(request, ct) is { } invalid)
        {
            return invalid;
        }

        var user = await users.GetByIdAsync(id, ct);
        if (user is null)
        {
            return Error.NotFound("User", id);
        }

        var role = await CheckRoleAsync(request.RoleId, keptRoleId: user.RoleId, ct);
        if (role.Error is { } badRole)
        {
            return badRole;
        }

        // Unverified is the system's (BR-01): the Admin chooses Active or Inactive.
        if (request.Status == UserStatus.Unverified && user.Status != UserStatus.Unverified)
        {
            return Error.Invalid(nameof(request.Status), "Choose Active or Inactive.");
        }

        var roleChanged = role.Value.Id != user.RoleId;
        var statusChanged = request.Status != user.Status;

        // E3: the Admin cannot lock itself out.
        if ((roleChanged || statusChanged) && user.Id == currentUser.Id)
        {
            return Error.Conflict("User.Self", "You cannot change the role or the status of the account you are signed in with.");
        }

        // A3: removing the Expert role releases the subjects the user owns, once the Admin agrees.
        IReadOnlyList<Subject> owned = [];
        if (roleChanged && user.Role.Value == Roles.Expert)
        {
            owned = await subjects.ListOwnedByAsync(user.Id, ct);
            if (owned.Count > 0 && !request.ConfirmHandOver)
            {
                return Error.Conflict("User.OwnsSubjects",
                    $"{user.FullName} owns {string.Join(", ", owned.Select(s => $"\"{s.Name}\""))}. " +
                    "Without the Expert role these subjects will have no owner until you assign one.");
            }

            foreach (var subject in owned)
            {
                subject.ExpertId = null;
            }
        }

        // A4: an Unverified account set Active counts as verified, and gets its password now (OI-16).
        string? password = null;
        if (user.Status == UserStatus.Unverified && request.Status == UserStatus.Active)
        {
            password = PasswordGenerator.Generate();
            user.PasswordHash = hasher.Hash(password);
        }

        // The authorization follows from the next request on: every request checks the role and the status (BR-03).
        user.Role = role.Value;
        user.Status = request.Status;
        await unitOfWork.SaveChangesAsync(ct);

        bool? sent = password is null ? null : await mail.SendAccountByAdminAsync(user.Email, user.FullName, password, ct);
        return new UserSavedDto(UserDto.From(user), sent, owned.Select(s => s.Name).ToList());
    }

    /// <summary>An active User Role; a stored account keeps its role even if switched off since (BR-16).</summary>
    private async Task<Result<Setting>> CheckRoleAsync(int roleId, int? keptRoleId, CancellationToken ct)
    {
        var role = await settings.GetByIdAsync(roleId, ct);
        if (role is null || role.Type != SettingType.UserRole || (role.Status == ActiveStatus.Inactive && role.Id != keptRoleId))
        {
            return Error.Invalid(nameof(CreateUserRequest.RoleId), "Choose a role that is in use.");
        }

        return role;
    }
}
