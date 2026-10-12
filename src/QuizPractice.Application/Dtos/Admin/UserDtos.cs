using QuizPractice.Application.Common;
using QuizPractice.Domain.Entities;
using QuizPractice.Domain.Enums;

namespace QuizPractice.Application.Dtos.Admin;

/// <summary>A setting offered in a drop-down: its id and its value.</summary>
public sealed record SettingLookupDto(int Id, string Name);

/// <summary>Criteria of Users List (UC-62). RoleId: a User Role setting. SortBy: id, fullName, gender, email, mobile, role or status.</summary>
public sealed record UserListRequest(
    string? Search = null,
    Gender? Gender = null,
    int? RoleId = null,
    UserStatus? Status = null,
    string? SortBy = null,
    bool Descending = false,
    int Page = 1,
    int PageSize = 10);

/// <summary>A row of Users List.</summary>
public sealed record UserSummaryDto(int Id, string FullName, Gender Gender, string Email, string Mobile, int RoleId, string Role, UserStatus Status)
{
    public static UserSummaryDto From(User u) => new(u.Id, u.FullName, u.Gender, u.Email, u.Mobile, u.RoleId, u.Role.Value, u.Status);
}

/// <summary>Choices of the list filters and of User Details: the user roles (BR-16).</summary>
public sealed record UserChoicesDto(IReadOnlyList<SettingLookupDto> Roles);

/// <summary>New account (UC-63 step 3): it starts Active; the password is generated and emailed.</summary>
public sealed record CreateUserRequest(
    string FullName,
    Gender Gender,
    string Email,
    string Mobile,
    string? Address,
    int RoleId,
    FileUpload? Avatar = null);

/// <summary>
/// A stored account (UC-63 step 3): only the role and the status change (BR-20).
/// ConfirmHandOver: the Admin agreed that the subjects of a former Expert lose their owner (A3).
/// </summary>
public sealed record UpdateUserRequest(int RoleId, UserStatus Status, bool ConfirmHandOver = false);

public sealed record UserDto(
    int Id,
    string? Avatar,
    string FullName,
    Gender Gender,
    string Email,
    string Mobile,
    string? Address,
    int RoleId,
    string Role,
    UserStatus Status)
{
    public static UserDto From(User u) =>
        new(u.Id, u.Avatar, u.FullName, u.Gender, u.Email, u.Mobile, u.Address, u.RoleId, u.Role.Value, u.Status);
}

/// <summary>
/// After a save. PasswordSent: null when no password was due; false when it was generated but
/// could not be emailed (E2) — the account is kept and the user can use Forgot Password.
/// ReleasedSubjects: the subjects that lost their owner (A3).
/// </summary>
public sealed record UserSavedDto(UserDto User, bool? PasswordSent, IReadOnlyList<string> ReleasedSubjects);
