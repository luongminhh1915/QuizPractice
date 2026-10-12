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
