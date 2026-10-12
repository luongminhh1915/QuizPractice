using QuizPractice.Application.Common;
using QuizPractice.Application.Dtos.Admin;
using QuizPractice.Domain.Common;

namespace QuizPractice.Application.Services.Admin;

/// <summary>UC-62 View Users List, UC-63 Add or Edit User.</summary>
public interface IUserService
{
    Task<Result<Page<UserSummaryDto>>> ListAsync(UserListRequest request, CancellationToken ct = default);

    Task<Result<UserChoicesDto>> GetChoicesAsync(CancellationToken ct = default);

    Task<Result<UserDto>> GetAsync(int id, CancellationToken ct = default);

    /// <summary>UC-63: a new Active account whose generated password is emailed (409 User.EmailTaken, E1).</summary>
    Task<Result<UserSavedDto>> CreateAsync(CreateUserRequest request, CancellationToken ct = default);

    /// <summary>UC-63: the role and the status of a stored account.</summary>
    Task<Result<UserSavedDto>> UpdateAsync(int id, UpdateUserRequest request, CancellationToken ct = default);
}
